from sqlalchemy import Column, Integer, String, DateTime, ForeignKey, func
from sqlalchemy.orm import relationship
from app.database import Base

class Invitee(Base):
    __tablename__ = "invitees"

    id = Column(Integer, primary_key=True, index=True)
    event_id = Column(Integer, ForeignKey("events.id", ondelete="CASCADE"), nullable=False)
    name = Column(String(100), nullable=False)
    phone = Column(String(20), nullable=False)
    email = Column(String(100), nullable=True)
    relation = Column(String(50), nullable=True) # friend, family, colleague, etc.
    invite_token = Column(String(100), unique=True, index=True, nullable=False)
    status = Column(String(20), default="sent")  # sent, opened, accepted, declined
    created_at = Column(DateTime(timezone=True), server_default=func.now())

    event = relationship("Event", back_populates="invitees")
    contributions = relationship("Contribution", back_populates="invitee", cascade="all, delete-orphan")
