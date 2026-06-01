from fastapi import APIRouter, Depends, HTTPException, Request, Header, status
from sqlalchemy.orm import Session
from app.database import get_db
from app.schemas.payment import PaymentVerification
from app.services.payment_service import payment_service
from app.utils.razorpay_client import razorpay_client
from app.models.payment import Payment
from app.services.gift_service import GiftService
import json
import logging

router = APIRouter(prefix="/api/payments", tags=["Payments"])
logger = logging.getLogger(__name__)

# 1. Verify Payment Signature (from Frontend checkout redirect/callback)
@router.post("/verify")
def verify_payment(
    payload: PaymentVerification,
    db: Session = Depends(get_db)
):
    success = payment_service.verify_payment(
        db=db,
        razorpay_order_id=payload.razorpay_order_id,
        razorpay_payment_id=payload.razorpay_payment_id,
        razorpay_signature=payload.razorpay_signature
    )
    
    if not success:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Payment verification failed or invalid signature"
        )
        
    return {"status": "success", "message": "Payment verified and contribution completed"}

# 2. Razorpay Webhook Endpoint
@router.post("/webhook")
async def razorpay_webhook(
    request: Request,
    x_razorpay_signature: str = Header(None),
    db: Session = Depends(get_db)
):
    """
    Listens to webhook alerts from Razorpay (e.g. payment.captured, payment.failed).
    Useful to handle disconnects or off-site flows.
    """
    if not x_razorpay_signature:
        raise HTTPException(status_code=400, detail="Signature header missing")

    body_bytes = await request.body()
    
    # 1. Verify Webhook Signature (bypass if mock/test settings)
    from app.config import settings
    is_mock = settings.RAZORPAY_KEY_ID == "rzp_test_xxx"
    
    if not is_mock:
        is_valid = razorpay_client.verify_webhook_signature(body_bytes, x_razorpay_signature)
        if not is_valid:
            raise HTTPException(status_code=400, detail="Invalid webhook signature")
            
    # 2. Process webhook event
    try:
        event_data = json.loads(body_bytes.decode('utf-8'))
        event_type = event_data.get("event")
        
        logger.info(f"Received Razorpay Webhook: {event_type}")
        
        if event_type == "payment.captured":
            payment_entity = event_data["payload"]["payment"]["entity"]
            order_id = payment_entity.get("order_id")
            payment_id = payment_entity.get("id")
            
            # Find payment record
            payment = db.query(Payment).filter(Payment.gateway_order_id == order_id).first()
            if payment:
                GiftService.process_successful_payment(
                    db=db,
                    payment_id=payment.id,
                    gateway_payment_id=payment_id,
                    gateway_signature=x_razorpay_signature
                )
                logger.info(f"Payment {payment_id} successfully captured via Webhook")
                
        elif event_type == "payment.failed":
            payment_entity = event_data["payload"]["payment"]["entity"]
            order_id = payment_entity.get("order_id")
            
            payment = db.query(Payment).filter(Payment.gateway_order_id == order_id).first()
            if payment:
                GiftService.process_failed_payment(db=db, payment_id=payment.id)
                logger.warning(f"Payment for order {order_id} failed via Webhook")
                
    except Exception as e:
        logger.error(f"Error processing Razorpay Webhook: {str(e)}")
        raise HTTPException(status_code=500, detail="Error parsing webhook payload")

    return {"status": "ok"}
