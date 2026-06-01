from fastapi import APIRouter, Depends, HTTPException, UploadFile, File, status
from sqlalchemy.orm import Session
from typing import List, Optional
import csv
import io
import uuid
from app.database import get_db
from app.schemas.invitee import InviteeCreate, InviteeResponse
from app.models.invitee import Invitee
from app.models.event import Event
from app.models.user import User
from app.services.auth_service import get_current_user

router = APIRouter(prefix="/api/events/{event_id}/invitees", tags=["Invitees"])

@router.post("", response_model=InviteeResponse, status_code=status.HTTP_201_CREATED)
def add_invitee(
    event_id: int,
    invitee_data: InviteeCreate,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_user)
):
    # Verify event belongs to host
    event = db.query(Event).filter(Event.id == event_id, Event.host_id == current_user.id).first()
    if not event:
        raise HTTPException(status_code=404, detail="Event not found or unauthorized")
        
    token = uuid.uuid4().hex
    db_invitee = Invitee(
        event_id=event_id,
        name=invitee_data.name,
        phone=invitee_data.phone,
        email=invitee_data.email,
        relation=invitee_data.relation,
        invite_token=token,
        status="sent"
    )
    db.add(db_invitee)
    db.commit()
    db.refresh(db_invitee)
    return db_invitee

@router.get("", response_model=List[InviteeResponse])
def get_invitees(
    event_id: int,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_user)
):
    # Verify event belongs to host
    event = db.query(Event).filter(Event.id == event_id, Event.host_id == current_user.id).first()
    if not event:
        raise HTTPException(status_code=404, detail="Event not found or unauthorized")
        
    return db.query(Invitee).filter(Invitee.event_id == event_id).all()

@router.post("/upload", response_model=List[InviteeResponse], status_code=status.HTTP_201_CREATED)
def bulk_upload_invitees(
    event_id: int,
    file: UploadFile = File(...),
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_user)
):
    # Verify event belongs to host
    event = db.query(Event).filter(Event.id == event_id, Event.host_id == current_user.id).first()
    if not event:
        raise HTTPException(status_code=404, detail="Event not found or unauthorized")

    # Read and parse file
    try:
        content = file.file.read().decode("utf-8-sig") # handles UTF-8 BOM
        csv_file = io.StringIO(content)
        reader = csv.DictReader(csv_file)
        
        # Strip header spaces
        reader.fieldnames = [name.strip().lower() for name in reader.fieldnames]
    except Exception as e:
        raise HTTPException(status_code=400, detail=f"Failed to read CSV: {str(e)}")

    required_cols = {"name", "phone"}
    if not required_cols.issubset(set(reader.fieldnames)):
        raise HTTPException(
            status_code=400,
            detail=f"CSV must contain at least 'name' and 'phone' columns. Found: {reader.fieldnames}"
        )

    added_invitees = []
    for row in reader:
        name = row.get("name")
        phone = row.get("phone")
        if not name or not phone:
            continue
            
        email = row.get("email")
        relation = row.get("relation")
        
        token = uuid.uuid4().hex
        db_invitee = Invitee(
            event_id=event_id,
            name=name.strip(),
            phone=phone.strip(),
            email=email.strip() if email else None,
            relation=relation.strip() if relation else None,
            invite_token=token,
            status="sent"
        )
        db.add(db_invitee)
        added_invitees.append(db_invitee)
        
    if not added_invitees:
        raise HTTPException(status_code=400, detail="No valid invitee records found in CSV")
        
    db.commit()
    for invitee in added_invitees:
        db.refresh(invitee)
        
    return added_invitees

@router.delete("/{invitee_id}", status_code=status.HTTP_204_NO_CONTENT)
def remove_invitee(
    event_id: int,
    invitee_id: int,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_user)
):
    event = db.query(Event).filter(Event.id == event_id, Event.host_id == current_user.id).first()
    if not event:
        raise HTTPException(status_code=404, detail="Event not found or unauthorized")
        
    invitee = db.query(Invitee).filter(Invitee.id == invitee_id, Invitee.event_id == event_id).first()
    if not invitee:
        raise HTTPException(status_code=404, detail="Invitee not found")
        
    db.delete(invitee)
    db.commit()
    return None

# Public verification route: get invitee details by token
@router.get("/token/{token}", response_model=InviteeResponse, tags=["Public"])
def get_invitee_by_token(
    token: str,
    db: Session = Depends(get_db)
):
    invitee = db.query(Invitee).filter(Invitee.invite_token == token).first()
    if not invitee:
        raise HTTPException(status_code=404, detail="Invalid invitation token")
        
    # Mark status as opened if not already accepted/declined
    if invitee.status == "sent":
        invitee.status = "opened"
        db.commit()
        db.refresh(invitee)
        
    return invitee
