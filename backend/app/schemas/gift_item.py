from pydantic import BaseModel, Field
from typing import Optional
from decimal import Decimal
from datetime import datetime

class GiftItemBase(BaseModel):
    name: str = Field(..., max_length=150)
    description: Optional[str] = None
    image_url: Optional[str] = None
    estimated_cost: Decimal = Field(..., gt=0)

class GiftItemCreate(GiftItemBase):
    pass

class GiftItemUpdate(BaseModel):
    name: Optional[str] = None
    description: Optional[str] = None
    image_url: Optional[str] = None
    estimated_cost: Optional[Decimal] = Field(None, gt=0)
    status: Optional[str] = None

class GiftItemResponse(GiftItemBase):
    id: int
    event_id: int
    contributed_amount: Decimal
    status: str
    created_at: datetime

    class Config:
        from_attributes = True
        json_encoders = {
            Decimal: lambda v: float(v)
        }
