from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.orm import Session
from typing import List, Optional
from app.database import get_db
from app.schemas.contribution import ContributionCreate, ContributionResponse
from app.models.gift_item import GiftItem
from app.models.invitee import Invitee
from app.models.event import Event
from app.models.contribution import Contribution
from app.models.user import User
from app.services.gift_service import GiftService
from app.services.payment_service import payment_service
from app.services.auth_service import get_current_user

router = APIRouter(tags=["Contributions"])

# 1. Initiate Contribution (Public / Invitee)
@router.post("/api/gifts/{gift_id}/contribute", status_code=status.HTTP_201_CREATED)
def contribute_to_gift(
    gift_id: int,
    payload: ContributionCreate,
    db: Session = Depends(get_db)
):
    # Retrieve invitee if token is provided
    invitee_id = None
    if payload.invitee_token:
        invitee = db.query(Invitee).filter(Invitee.invite_token == payload.invitee_token).first()
        if not invitee:
            raise HTTPException(status_code=400, detail="Invalid invitation token")
        invitee_id = invitee.id

    # 1. Lock gift item and initiate contribution/payment record
    # This also performs remaining amount and status validations.
    contribution, payment = GiftService.initiate_contribution(
        db=db,
        gift_id=gift_id,
        amount=payload.amount,
        invitee_id=invitee_id,
        anonymous=payload.anonymous
    )

    # 2. Get details of the gift item to pass details
    gift = db.query(GiftItem).filter(GiftItem.id == gift_id).first()

    # 3. Create order on Razorpay
    order_details = payment_service.create_razorpay_order(
        db=db,
        payment=payment,
        gift_name=gift.name
    )

    return {
        "contribution": {
            "id": contribution.id,
            "gift_item_id": contribution.gift_item_id,
            "amount": float(contribution.amount),
            "anonymous": contribution.anonymous,
            "status": contribution.status,
            "created_at": contribution.created_at
        },
        "razorpay_order": order_details
    }

# 2. List all contributions for an event (Host Auth)
@router.get("/api/events/{event_id}/contributions", response_model=List[ContributionResponse])
def list_event_contributions(
    event_id: int,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_user)
):
    # Verify event belongs to host
    event = db.query(Event).filter(Event.id == event_id, Event.host_id == current_user.id).first()
    if not event:
        raise HTTPException(status_code=404, detail="Event not found or unauthorized")

    contributions = (
        db.query(Contribution)
        .join(GiftItem)
        .filter(GiftItem.event_id == event_id)
        .all()
    )

    # Attach invitee names directly to response models
    res = []
    for c in contributions:
        c_res = ContributionResponse.model_validate(c)
        if c.invitee:
            c_res.invitee_name = c.invitee.name
        else:
            c_res.invitee_name = "Direct Contributor"
        res.append(c_res)
        
    return res

# 3. Public Contribution Feed for an Event (Public) - masks anonymous guest names
@router.get("/api/events/{event_id}/public-contributions")
def get_public_contribution_feed(
    event_id: int,
    db: Session = Depends(get_db)
):
    contributions = (
        db.query(Contribution)
        .join(GiftItem)
        .filter(GiftItem.event_id == event_id, Contribution.status == "SUCCESS")
        .order_by(Contribution.created_at.desc())
        .all()
    )

    res = []
    for c in contributions:
        # Mask name if anonymous
        if c.anonymous:
            display_name = "Anonymous"
        else:
            display_name = c.invitee.name if c.invitee else "Well Wisher"

        res.append({
            "id": c.id,
            "gift_item_id": c.gift_item_id,
            "gift_item_name": c.gift_item.name,
            "amount": float(c.amount),
            "display_name": display_name,
            "created_at": c.created_at
        })
    return res
