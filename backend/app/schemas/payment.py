from pydantic import BaseModel
from typing import Optional
from decimal import Decimal
from datetime import datetime

class PaymentCreate(BaseModel):
    amount: Decimal
    currency: str = "INR"

class PaymentResponse(BaseModel):
    id: int
    gateway: str
    gateway_order_id: str
    gateway_payment_id: Optional[str]
    amount: Decimal
    currency: str
    status: str
    created_at: datetime

    class Config:
        from_attributes = True

class RazorpayOrderResponse(BaseModel):
    order_id: str
    amount: int  # in paise
    currency: str
    key_id: str

class PaymentVerification(BaseModel):
    razorpay_order_id: str
    razorpay_payment_id: str
    razorpay_signature: str
    contribution_id: int
