from fastapi import FastAPI, Request
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from fastapi.responses import JSONResponse
import os

from app.config import settings
from app.database import engine, Base

# Import models so Base.metadata is aware of them
from app.models import user, event, invitee, gift_item, payment, contribution

# Create DB tables on startup
Base.metadata.create_all(bind=engine)

# Import routers
from app.routers import auth, events, invitees, gifts, contributions, payments, reports, admin

app = FastAPI(
    title=settings.APP_NAME,
    description="Wedding Gift Contribution Platform API",
    version="1.0.0",
    docs_url="/docs" if settings.DEBUG else None
)

# CORS setup
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    #llow_origins=settings.cors_origins_list,
    allow_credentials=False,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Ensure upload directory exists
os.makedirs(settings.UPLOAD_DIR, exist_ok=True)
os.makedirs(os.path.join(settings.UPLOAD_DIR, "gifts"), exist_ok=True)

# Mount media static files directory for file storage URLs
app.mount("/uploads", StaticFiles(directory="uploads"), name="uploads")

app.mount("/static", StaticFiles(directory="dist", html=True), name="frontend")

# Include routers
app.include_router(auth.router)
app.include_router(events.router)
app.include_router(invitees.router)
app.include_router(gifts.router)
app.include_router(contributions.router)
app.include_router(payments.router)
app.include_router(reports.router)
app.include_router(admin.router)

@app.get("/health")
def health_check():
    return {"status": "healthy", "service": settings.APP_NAME}

# Custom exception handler for debug info
@app.exception_handler(Exception)
async def global_exception_handler(request: Request, exc: Exception):
    return JSONResponse(
        status_code=500,
        content={"detail": str(exc) if settings.DEBUG else "Internal server error"}
    )
