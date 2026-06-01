from sqlalchemy import Column, Integer, String, Numeric, DateTime, ForeignKey, func, Text
from sqlalchemy.orm import relationship
from app.database import Base

class GiftItem(Base):
    __tablename__ = "gift_items"

    id = Column(Integer, primary_key=True, index=True)
    event_id = Column(Integer, ForeignKey("events.id", ondelete="CASCADE"), nullable=False)
    name = Column(String(150), nullable=False)
    description = Column(Text, nullable=True)
    image_url = Column(String(255), nullable=True)
    estimated_cost = Column(Numeric(12, 2), nullable=False)
    contributed_amount = Column(Numeric(12, 2), default=0.00, nullable=False)
    status = Column(String(20), default="OPEN")  # OPEN, FUNDED, CLOSED
    created_at = Column(DateTime(timezone=True), server_default=func.now())

    event = relationship("Event", back_populates="gifts")
    contributions = relationship("Contribution", back_populates="gift_item", cascade="all, delete-orphan")
