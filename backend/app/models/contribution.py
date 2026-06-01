from sqlalchemy import Column, Integer, String, Numeric, DateTime, ForeignKey, Boolean, func
from sqlalchemy.orm import relationship
from app.database import Base

class Contribution(Base):
    __tablename__ = "contributions"

    id = Column(Integer, primary_key=True, index=True)
    gift_item_id = Column(Integer, ForeignKey("gift_items.id", ondelete="CASCADE"), nullable=False)
    invitee_id = Column(Integer, ForeignKey("invitees.id", ondelete="SET NULL"), nullable=True) # None means external/direct gift invite
    amount = Column(Numeric(12, 2), nullable=False)
    payment_id = Column(Integer, ForeignKey("payments.id", ondelete="SET NULL"), nullable=True)
    anonymous = Column(Boolean, default=False, nullable=False)
    status = Column(String(20), default="PENDING")  # PENDING, SUCCESS, FAILED, REFUNDED
    created_at = Column(DateTime(timezone=True), server_default=func.now())

    gift_item = relationship("GiftItem", back_populates="contributions")
    invitee = relationship("Invitee", back_populates="contributions")
    payment = relationship("Payment", back_populates="contributions")
