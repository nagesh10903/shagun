from sqlalchemy.orm import Session
from app.models.gift_item import GiftItem
from app.models.contribution import Contribution
from app.models.payment import Payment
from app.services.notification_service import notification_service
from decimal import Decimal
from fastapi import HTTPException, status
import logging

logger = logging.getLogger(__name__)

class GiftService:
    @staticmethod
    def initiate_contribution(
        db: Session,
        gift_id: int,
        amount: Decimal,
        invitee_id: int = None,
        anonymous: bool = False
    ) -> tuple[Contribution, Payment]:
        """
        Locks the gift item to perform checks, verifies that it is not already funded
        and that the contribution does not exceed the remaining balance.
        Then creates PENDING contribution and payment records.
        """
        # Lock row for safety checks
        gift = db.query(GiftItem).with_for_update().filter(GiftItem.id == gift_id).first()
        if not gift:
            raise HTTPException(status_code=404, detail="Gift item not found")
        
        if gift.status == "FUNDED":
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail="Gift already funded"
            )

        remaining = gift.estimated_cost - gift.contributed_amount
        if amount > remaining:
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail=f"Contribution exceeds balance. Remaining: ₹{remaining:,.2f}"
            )

        # Generate local Payment record
        payment = Payment(
            gateway="Razorpay",
            gateway_order_id="local_" + str(gift_id) + "_" + str(amount), # Placeholder, updated when order is created
            amount=amount,
            currency="INR",
            status="PENDING"
        )
        db.add(payment)
        db.flush() # get payment ID

        # Generate Contribution record
        contribution = Contribution(
            gift_item_id=gift_id,
            invitee_id=invitee_id,
            amount=amount,
            payment_id=payment.id,
            anonymous=anonymous,
            status="PENDING"
        )
        db.add(contribution)
        db.commit()

        return contribution, payment

    @staticmethod
    def process_successful_payment(
        db: Session,
        payment_id: int,
        gateway_payment_id: str,
        gateway_signature: str = None
    ) -> bool:
        """
        Applies successful payment updates inside a database transaction, 
        using row locks on the GiftItem to prevent overfunding.
        """
        payment = db.query(Payment).filter(Payment.id == payment_id).first()
        if not payment:
            logger.error(f"Payment with ID {payment_id} not found")
            return False

        if payment.status == "SUCCESS":
            return True # already processed

        # Find associated contributions
        contributions = db.query(Contribution).filter(Contribution.payment_id == payment.id).all()
        if not contributions:
            logger.error(f"No contributions found for payment ID {payment_id}")
            return False

        # Lock gift items first (always lock resources in a consistent order)
        # Usually it's 1 contribution to 1 gift_item, but let's lock the gift item
        for contribution in contributions:
            gift = db.query(GiftItem).with_for_update().filter(GiftItem.id == contribution.gift_item_id).first()
            if not gift:
                logger.error(f"Gift item {contribution.gift_item_id} not found during payment processing")
                db.rollback()
                return False

            # Recalculate remaining amount to avoid race conditions
            remaining = gift.estimated_cost - gift.contributed_amount
            if contribution.amount > remaining:
                # If a concurrent payment happened, it might exceed remaining now
                logger.error(f"Race condition occurred: Contribution amount {contribution.amount} exceeds remaining {remaining}")
                # Update statuses to FAILED or handle refund process
                payment.status = "FAILED"
                contribution.status = "FAILED"
                db.commit()
                return False

            # Update contribution status
            contribution.status = "SUCCESS"

            # Increment contributed amount
            gift.contributed_amount += contribution.amount

            # If funded, close the gift
            if gift.contributed_amount >= gift.estimated_cost:
                gift.status = "FUNDED"

            # Trigger notification
            invitee_name = "Anonymous"
            invitee_phone = "N/A"
            invitee_email = None
            if contribution.invitee:
                invitee_name = contribution.invitee.name
                invitee_phone = contribution.invitee.phone
                invitee_email = contribution.invitee.email
                
            notification_service.send_thank_you(
                to_phone=invitee_phone,
                to_email=invitee_email,
                invitee_name=invitee_name,
                amount=float(contribution.amount),
                gift_name=gift.name
            )

        # Update payment status
        payment.status = "SUCCESS"
        payment.gateway_payment_id = gateway_payment_id
        if gateway_signature:
            payment.gateway_signature = gateway_signature

        db.commit()
        return True

    @staticmethod
    def process_failed_payment(db: Session, payment_id: int):
        payment = db.query(Payment).filter(Payment.id == payment_id).first()
        if payment and payment.status == "PENDING":
            payment.status = "FAILED"
            db.query(Contribution).filter(Contribution.payment_id == payment.id).update({"status": "FAILED"})
            db.commit()
