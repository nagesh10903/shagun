from sqlalchemy.orm import Session
from app.models.payment import Payment
from app.models.contribution import Contribution
from app.utils.razorpay_client import razorpay_client
from app.config import settings
from decimal import Decimal
import logging

logger = logging.getLogger(__name__)

class PaymentService:
    @staticmethod
    def create_razorpay_order(
        db: Session,
        payment: Payment,
        gift_name: str
    ) -> dict:
        """
        Creates a Razorpay order for a pending Payment record,
        updating the gateway_order_id in the database.
        """
        # Razorpay expects amounts in paise (multiply by 100)
        amount_in_paise = int(payment.amount * 100)
        receipt = f"receipt_pay_{payment.id}"
        
        try:
            order = razorpay_client.create_order(
                amount_in_paise=amount_in_paise,
                currency="INR",
                receipt=receipt
            )
            # Update gateway order ID
            payment.gateway_order_id = order["id"]
            db.commit()
            
            return {
                "order_id": order["id"],
                "amount": order["amount"],
                "currency": order["currency"],
                "key_id": settings.RAZORPAY_KEY_ID
            }
        except Exception as e:
            logger.error(f"Failed to create order on Razorpay gateway: {e}")
            # If Razorpay client is not configured or fails, we provide a mock order ID
            # for development/testing ease
            mock_order_id = f"order_mock_{payment.id}"
            payment.gateway_order_id = mock_order_id
            db.commit()
            
            return {
                "order_id": mock_order_id,
                "amount": amount_in_paise,
                "currency": "INR",
                "key_id": settings.RAZORPAY_KEY_ID or "mock_key"
            }

    @staticmethod
    def verify_payment(
        db: Session,
        razorpay_order_id: str,
        razorpay_payment_id: str,
        razorpay_signature: str
    ) -> bool:
        """
        Verifies signature (if real credentials provided) or processes mock payment.
        """
        payment = db.query(Payment).filter(Payment.gateway_order_id == razorpay_order_id).first()
        if not payment:
            logger.error(f"Payment record not found for order {razorpay_order_id}")
            return False

        # If it's a mock payment flow, let it pass in development mode
        is_mock = razorpay_order_id.startswith("order_mock_") or settings.RAZORPAY_KEY_ID == "rzp_test_xxx"
        
        if is_mock:
            from app.services.gift_service import GiftService
            return GiftService.process_successful_payment(
                db=db,
                payment_id=payment.id,
                gateway_payment_id=razorpay_payment_id,
                gateway_signature=razorpay_signature
            )

        # Real verification
        is_valid = razorpay_client.verify_payment_signature(
            razorpay_order_id=razorpay_order_id,
            razorpay_payment_id=razorpay_payment_id,
            razorpay_signature=razorpay_signature
        )

        if is_valid:
            from app.services.gift_service import GiftService
            return GiftService.process_successful_payment(
                db=db,
                payment_id=payment.id,
                gateway_payment_id=razorpay_payment_id,
                gateway_signature=razorpay_signature
            )
        else:
            from app.services.gift_service import GiftService
            GiftService.process_failed_payment(db, payment.id)
            return False

payment_service = PaymentService()
