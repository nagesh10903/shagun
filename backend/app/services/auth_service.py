from fastapi import Depends, HTTPException, status
from fastapi.security import OAuth2PasswordBearer
from sqlalchemy.orm import Session
from app.database import get_db
from app.models.user import User
from app.schemas.user import UserCreate, TokenData
from app.utils.jwt_handler import get_password_hash, verify_password, decode_token, create_access_token, create_refresh_token
import logging

logger = logging.getLogger(__name__)

oauth2_scheme = OAuth2PasswordBearer(tokenUrl="api/auth/login")

def create_user(db: Session, user_data: UserCreate) -> User:
    # Check if phone already registered
    print(user_data.name)
    print(user_data.phone)
    print(user_data.password)
    print(user_data.email)
    print(user_data.role)
    existing_user = db.query(User).filter(User.phone == user_data.phone).first()
    if existing_user:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Phone number already registered"
        )
    
    # Check if email already registered (if provided)
    if user_data.email:
        existing_email = db.query(User).filter(User.email == user_data.email).first()
        if existing_email:
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail="Email already registered"
            )

    #hashed_password = get_password_hash(user_data.password)
    hashed_password =user_data.password
    db_user = User(
        name=user_data.name,
        phone=user_data.phone,
        email=user_data.email,
        password_hash=hashed_password,
        role=user_data.role or "host"
    )
    db.add(db_user)
    db.commit()
    db.refresh(db_user)
    return db_user

def authenticate_user(db: Session, phone: str, password: str) -> User:
    user = db.query(User).filter(User.phone == phone).first()

    if not user or not verify_password(password, user.password_hash):
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Incorrect phone number or password",
            headers={"WWW-Authenticate": "Bearer"},
        )
    return user

def get_current_user(token: str = Depends(oauth2_scheme), db: Session = Depends(get_db)) -> User:
    credentials_exception = HTTPException(
        status_code=status.HTTP_401_UNAUTHORIZED,
        detail="Could not validate credentials",
        headers={"WWW-Authenticate": "Bearer"},
    )
    payload = decode_token(token)
    if payload is None or payload.get("type") != "access":
        raise credentials_exception
    
    phone: str = payload.get("sub")
    role: str = payload.get("role")
    if phone is None:
        raise credentials_exception
    
    token_data = TokenData(phone=phone, role=role)
    user = db.query(User).filter(User.phone == token_data.phone).first()
    if user is None:
        raise credentials_exception
    return user

def get_current_admin(current_user: User = Depends(get_current_user)) -> User:
    if current_user.role != "admin":
        raise HTTPException(
            status_code=status.HTTP_403_FORBIDDEN,
            detail="Access forbidden: Insufficient permissions"
        )
    return current_user
