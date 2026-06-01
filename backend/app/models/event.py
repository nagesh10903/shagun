from sqlalchemy import Column, Integer, String, Date, DateTime, ForeignKey, func, Text
from sqlalchemy.orm import relationship
from app.database import Base

class Event(Base):
    __tablename__ = "events"

    id = Column(Integer, primary_key=True, index=True)
    host_id = Column(Integer, ForeignKey("users.id", ondelete="CASCADE"), nullable=False)
    event_name = Column(String(150), nullable=False)
    groom_name = Column(String(100), nullable=False)
    bride_name = Column(String(100), nullable=False)
    event_date = Column(Date, nullable=False)
    venue = Column(String(255), nullable=False)
    description = Column(Text, nullable=True)
    cover_photo_url = Column(String(255), nullable=True)
    status = Column(String(20), default="active")  # active, completed, cancelled
    created_at = Column(DateTime(timezone=True), server_default=func.now())

    host = relationship("User", back_populates="events")
    invitees = relationship("Invitee", back_populates="event", cascade="all, delete-orphan")
    gifts = relationship("GiftItem", back_populates="event", cascade="all, delete-orphan")
