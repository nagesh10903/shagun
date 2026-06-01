from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.orm import Session
from typing import List
from app.database import get_db
from app.models.user import User
from app.models.event import Event
from app.models.payment import Payment
from app.schemas.user import UserResponse
from app.schemas.event import EventResponse
from app.schemas.payment import PaymentResponse
from app.services.auth_service import get_current_admin

router = APIRouter(prefix="/api/admin", tags=["Admin Operations"])

# 1. List Users
@router.get("/users", response_model=List[UserResponse])
def get_all_users(
    db: Session = Depends(get_db),
    admin: User = Depends(get_current_admin)
):
    return db.query(User).all()

# 2. List Events
@router.get("/events", response_model=List[EventResponse])
def get_all_events(
    db: Session = Depends(get_db),
    admin: User = Depends(get_current_admin)
):
    return db.query(Event).all()

# 3. List Transactions / Payments
@router.get("/payments", response_model=List[PaymentResponse])
def get_all_payments(
    db: Session = Depends(get_db),
    admin: User = Depends(get_current_admin)
):
    return db.query(Payment).all()

# 4. Update User Role
@router.put("/users/{user_id}/role", response_model=UserResponse)
def update_user_role(
    user_id: int,
    role: str,
    db: Session = Depends(get_db),
    admin: User = Depends(get_current_admin)
):
    if role not in ["host", "admin"]:
        raise HTTPException(status_code=400, detail="Invalid role. Must be 'host' or 'admin'")
        
    user = db.query(User).filter(User.id == user_id).first()
    if not user:
        raise HTTPException(status_code=404, detail="User not found")
        
    user.role = role
    db.commit()
    db.refresh(user)
    return user
