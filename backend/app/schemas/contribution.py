from pydantic import BaseModel, Field
from typing import Optional
from decimal import Decimal
from datetime import datetime

class ContributionBase(BaseModel):
    amount: Decimal = Field(..., gt=0)
    anonymous: bool = False

class ContributionCreate(ContributionBase):
    invitee_token: Optional[str] = None # Invite link context

class ContributionResponse(ContributionBase):
    id: int
    gift_item_id: int
    invitee_id: Optional[int]
    payment_id: Optional[int]
    status: str
    created_at: datetime
    # We will conditionally mask invitee details in the API layer based on anonymous flag and user authentication
    invitee_name: Optional[str] = None 

    class Config:
        from_attributes = True
        json_encoders = {
            Decimal: lambda v: float(v)
        }
