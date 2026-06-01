from sqlalchemy import Column, Integer, String, Numeric, DateTime, func
from sqlalchemy.orm import relationship
from app.database import Base

class Payment(Base):
    __tablename__ = "payments"

    id = Column(Integer, primary_key=True, index=True)
    gateway = Column(String(50), default="Razorpay", nullable=False)
    gateway_order_id = Column(String(100), unique=True, index=True, nullable=False)
    gateway_payment_id = Column(String(100), unique=True, index=True, nullable=True)
    gateway_signature = Column(String(255), nullable=True)
    amount = Column(Numeric(12, 2), nullable=False)
    currency = Column(String(10), default="INR", nullable=False)
    status = Column(String(20), default="PENDING")  # PENDING, SUCCESS, FAILED, REFUNDED
    created_at = Column(DateTime(timezone=True), server_default=func.now())

    contributions = relationship("Contribution", back_populates="payment")
