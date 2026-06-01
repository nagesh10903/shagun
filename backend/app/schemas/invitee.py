from pydantic import BaseModel, Field, EmailStr
from typing import Optional
from datetime import datetime

class InviteeBase(BaseModel):
    name: str = Field(..., max_length=100)
    phone: str = Field(..., max_length=20)
    email: Optional[EmailStr] = None
    relation: Optional[str] = Field(None, max_length=50)

class InviteeCreate(InviteeBase):
    pass

class InviteeResponse(InviteeBase):
    id: int
    event_id: int
    invite_token: str
    status: str
    created_at: datetime

    class Config:
        from_attributes = True

class InviteeBulkUpload(BaseModel):
    name: str
    phone: str
    email: Optional[str] = None
    relation: Optional[str] = None
