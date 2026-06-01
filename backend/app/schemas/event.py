from pydantic import BaseModel, Field
from typing import Optional
from datetime import date, datetime

class EventBase(BaseModel):
    event_name: str = Field(..., max_length=150)
    groom_name: str = Field(..., max_length=100)
    bride_name: str = Field(..., max_length=100)
    event_date: date
    venue: str = Field(..., max_length=255)
    description: Optional[str] = None
    cover_photo_url: Optional[str] = None
    status: Optional[str] = "active"

class EventCreate(EventBase):
    pass

class EventUpdate(BaseModel):
    event_name: Optional[str] = None
    groom_name: Optional[str] = None
    bride_name: Optional[str] = None
    event_date: Optional[date] = None
    venue: Optional[str] = None
    description: Optional[str] = None
    cover_photo_url: Optional[str] = None
    status: Optional[str] = None

class EventResponse(EventBase):
    id: int
    host_id: int
    created_at: datetime

    class Config:
        from_attributes = True
