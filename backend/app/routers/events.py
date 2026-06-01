from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.orm import Session
from typing import List
from app.database import get_db
from app.schemas.event import EventCreate, EventUpdate, EventResponse
from app.models.event import Event
from app.models.user import User
from app.services.auth_service import get_current_user

router = APIRouter(prefix="/api/events", tags=["Events"])

@router.post("", response_model=EventResponse, status_code=status.HTTP_201_CREATED)
def create_event(
    event_data: EventCreate,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_user)
):
    db_event = Event(
        host_id=current_user.id,
        event_name=event_data.event_name,
        groom_name=event_data.groom_name,
        bride_name=event_data.bride_name,
        event_date=event_data.event_date,
        venue=event_data.venue,
        description=event_data.description,
        cover_photo_url=event_data.cover_photo_url,
        status=event_data.status or "active"
    )
    db.add(db_event)
    db.commit()
    db.refresh(db_event)
    return db_event

@router.get("", response_model=List[EventResponse])
def get_user_events(
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_user)
):
    return db.query(Event).filter(Event.host_id == current_user.id).all()

@router.get("/{event_id}", response_model=EventResponse)
def get_event(
    event_id: int,
    db: Session = Depends(get_db)
):
    event = db.query(Event).filter(Event.id == event_id).first()
    if not event:
        raise HTTPException(status_code=404, detail="Event not found")
    return event

@router.put("/{event_id}", response_model=EventResponse)
def update_event(
    event_id: int,
    event_data: EventUpdate,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_user)
):
    event = db.query(Event).filter(Event.id == event_id, Event.host_id == current_user.id).first()
    if not event:
        raise HTTPException(status_code=404, detail="Event not found or unauthorized")
    
    update_dict = event_data.model_dump(exclude_unset=True)
    for key, value in update_dict.items():
        setattr(event, key, value)
        
    db.commit()
    db.refresh(event)
    return event

@router.delete("/{event_id}", status_code=status.HTTP_204_NO_CONTENT)
def delete_event(
    event_id: int,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_user)
):
    event = db.query(Event).filter(Event.id == event_id, Event.host_id == current_user.id).first()
    if not event:
        raise HTTPException(status_code=404, detail="Event not found or unauthorized")
    db.delete(event)
    db.commit()
    return None
