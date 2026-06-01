from fastapi import APIRouter, Depends, HTTPException, status
from fastapi.responses import StreamingResponse
from sqlalchemy.orm import Session
from sqlalchemy import func
from app.database import get_db
from app.models.event import Event
from app.models.gift_item import GiftItem
from app.models.contribution import Contribution
from app.models.invitee import Invitee
from app.models.user import User
from app.services.auth_service import get_current_user
import io
import csv

router = APIRouter(prefix="/api/events/{event_id}/reports", tags=["Reports"])

# 1. Summary Metrics
@router.get("/summary")
def get_event_summary(
    event_id: int,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_user)
):
    # Verify event host
    event = db.query(Event).filter(Event.id == event_id, Event.host_id == current_user.id).first()
    if not event:
        raise HTTPException(status_code=404, detail="Event not found or unauthorized")

    # Metrics queries
    total_gifts = db.query(func.count(GiftItem.id)).filter(GiftItem.event_id == event_id).scalar() or 0
    funded_gifts = db.query(func.count(GiftItem.id)).filter(GiftItem.event_id == event_id, GiftItem.status == "FUNDED").scalar() or 0
    total_invitees = db.query(func.count(Invitee.id)).filter(Invitee.event_id == event_id).scalar() or 0

    # Total cash expected vs received
    cost_received = db.query(
        func.sum(GiftItem.estimated_cost),
        func.sum(GiftItem.contributed_amount)
    ).filter(GiftItem.event_id == event_id).first()

    target_amount = float(cost_received[0]) if cost_received and cost_received[0] else 0.0
    received_amount = float(cost_received[1]) if cost_received and cost_received[1] else 0.0
    remaining_amount = max(0.0, target_amount - received_amount)

    funding_percentage = (received_amount / target_amount * 100) if target_amount > 0 else 0.0

    return {
        "total_gifts": total_gifts,
        "funded_gifts": funded_gifts,
        "total_invitees": total_invitees,
        "target_amount": target_amount,
        "received_amount": received_amount,
        "remaining_amount": remaining_amount,
        "funding_percentage": round(funding_percentage, 1)
    }

# 2. Export Contribution List (CSV Download)
@router.get("/export")
def export_contribution_report(
    event_id: int,
    db: Session = Depends(get_db),
    current_user: User = Depends(get_current_user)
):
    # Verify event host
    event = db.query(Event).filter(Event.id == event_id, Event.host_id == current_user.id).first()
    if not event:
        raise HTTPException(status_code=404, detail="Event not found or unauthorized")

    contributions = (
        db.query(Contribution)
        .join(GiftItem)
        .filter(GiftItem.event_id == event_id)
        .all()
    )

    output = io.StringIO()
    writer = csv.writer(output)
    
    # Headers
    writer.writerow([
        "Contribution ID", 
        "Contributor Name", 
        "Phone Number", 
        "Email", 
        "Gift Item Name", 
        "Amount (₹)", 
        "Payment ID", 
        "Anonymous", 
        "Status", 
        "Created At"
    ])

    for c in contributions:
        name = c.invitee.name if c.invitee else "Direct contributor"
        phone = c.invitee.phone if c.invitee else "N/A"
        email = c.invitee.email if c.invitee else "N/A"
        
        writer.writerow([
            c.id,
            name,
            phone,
            email,
            c.gift_item.name,
            float(c.amount),
            c.payment.gateway_payment_id if c.payment else "N/A",
            "Yes" if c.anonymous else "No",
            c.status,
            c.created_at.strftime("%Y-%m-%d %H:%M:%S") if c.created_at else ""
        ])

    output.seek(0)
    filename = f"contributions_event_{event_id}.csv"
    
    return StreamingResponse(
        io.BytesIO(output.getvalue().encode("utf-8")),
        media_type="text/csv",
        headers={"Content-Disposition": f"attachment; filename={filename}"}
    )
