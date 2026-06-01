from fastapi import APIRouter, Depends, HTTPException, UploadFile, File, status
from sqlalchemy.orm import Session
from typing import List, Optional
from app.database import get_db
from app.schemas.gift_item import GiftItemCreate, GiftItemUpdate, GiftItemResponse
from app.models.gift_item import GiftItem
from app.models.event import Event
from app.models.user import User
from app.services.auth_service import get_current_user
from app.utils.file_handler import save_uploaded_file

router = APIRouter(tags=["Gifts"])

# 1. Upload Image (Host Auth)
@router.post("/api/gifts/upload-image", response_model=dict)
def upload_gift_image(
    file: UploadFile = File(...),
    current_user: User = Depends(get_current_user)
):
    url = save_uploaded_file(file, subfolder="gifts")
    return {"image_url": url}

# 2. Add Gift Item (Host Auth)
@router.post("/api/events/{event_id}/gifts", response_model=GiftItemResponse, status_code=status.HTTP_201_CREATED)
def create_gift_item(
    event_id: int,
    gift_data: GiftItemCreate,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_user)
):
    event = db.query(Event).filter(Event.id == event_id, Event.host_id == current_user.id).first()
    if not event:
        raise HTTPException(status_code=404, detail="Event not found or unauthorized")
        
    db_gift = GiftItem(
        event_id=event_id,
        name=gift_data.name,
        description=gift_data.description,
        image_url=gift_data.image_url,
        estimated_cost=gift_data.estimated_cost,
        contributed_amount=0.00,
        status="OPEN"
    )
    db.add(db_gift)
    db.commit()
    db.refresh(db_gift)
    return db_gift

# 3. List Gifts for an Event (Public)
@router.get("/api/events/{event_id}/gifts", response_model=List[GiftItemResponse])
def list_gifts(
    event_id: int,
    db: Session = Depends(get_db)
):
    return db.query(GiftItem).filter(GiftItem.event_id == event_id).all()

# 4. Gift details (Public)
@router.get("/api/gifts/{gift_id}", response_model=GiftItemResponse)
def get_gift(
    gift_id: int,
    db: Session = Depends(get_db)
):
    gift = db.query(GiftItem).filter(GiftItem.id == gift_id).first()
    if not gift:
        raise HTTPException(status_code=404, detail="Gift item not found")
    return gift

# 5. Update Gift (Host Auth)
@router.put("/api/gifts/{gift_id}", response_model=GiftItemResponse)
def update_gift(
    gift_id: int,
    gift_data: GiftItemUpdate,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_user)
):
    gift = db.query(GiftItem).filter(GiftItem.id == gift_id).first()
    if not gift:
        raise HTTPException(status_code=404, detail="Gift item not found")
        
    # Verify event host
    event = db.query(Event).filter(Event.id == gift.event_id, Event.host_id == current_user.id).first()
    if not event:
        raise HTTPException(status_code=403, detail="Unauthorized to modify this gift item")
        
    update_dict = gift_data.model_dump(exclude_unset=True)
    for key, value in update_dict.items():
        # Prevent manual changes to contributed_amount directly via update endpoint
        if key == "contributed_amount":
            continue
        setattr(gift, key, value)
        
    # Re-evaluate status if cost was updated
    if gift.contributed_amount >= gift.estimated_cost:
        gift.status = "FUNDED"
    elif gift.status == "FUNDED" and gift.contributed_amount < gift.estimated_cost:
        # Re-open if host increased estimated cost
        gift.status = "OPEN"
        
    db.commit()
    db.refresh(gift)
    return gift

# 6. Delete Gift (Host Auth)
@router.delete("/api/gifts/{gift_id}", status_code=status.HTTP_204_NO_CONTENT)
def delete_gift(
    gift_id: int,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_user)
):
    gift = db.query(GiftItem).filter(GiftItem.id == gift_id).first()
    if not gift:
        raise HTTPException(status_code=404, detail="Gift item not found")
        
    # Verify event host
    event = db.query(Event).filter(Event.id == gift.event_id, Event.host_id == current_user.id).first()
    if not event:
        raise HTTPException(status_code=403, detail="Unauthorized to delete this gift item")
        
    db.delete(gift)
    db.commit()
    return None
