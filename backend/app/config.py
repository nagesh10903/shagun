# pyrefly: ignore [missing-import]
from pydantic_settings import BaseSettings
from typing import List


class Settings(BaseSettings):
    APP_NAME: str = "Shagun"
    APP_ENV: str = "development"
    DEBUG: bool = True
    SECRET_KEY: str = "changeme"

    DATABASE_URL: str = "mysql://nagesh:test123@localhost/shagun"

    JWT_SECRET_KEY: str = "shagun-jwt-secret-key-development"
    JWT_ALGORITHM: str = "HS256"
    JWT_ACCESS_TOKEN_EXPIRE_MINUTES: int = 60
    JWT_REFRESH_TOKEN_EXPIRE_DAYS: int = 7

    RAZORPAY_KEY_ID: str = "rzp_test_xxx"
    RAZORPAY_KEY_SECRET: str = "xxx"

    CORS_ORIGINS: str = "http://localhost:3000,http://localhost:5173"

    UPLOAD_DIR: str = "uploads"
    MAX_UPLOAD_SIZE_MB: int = 5

    SMTP_HOST: str = "smtp.gmail.com"
    SMTP_PORT: int = 587
    SMTP_USER: str = ""
    SMTP_PASS: str = ""
    FROM_EMAIL: str = "noreply@shagun.app"

    FRONTEND_URL: str = "http://localhost:5173"

    @property
    def cors_origins_list(self) -> List[str]:
        return [o.strip() for o in self.CORS_ORIGINS.split(",")]

    class Config:
        env_file = ".env"
        extra = "ignore"


settings = Settings()
