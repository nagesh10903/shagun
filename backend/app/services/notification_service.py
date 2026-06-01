import logging
from typing import Optional

logger = logging.getLogger(__name__)

class NotificationService:
    @staticmethod
    def send_thank_you(
        to_phone: str,
        to_email: Optional[str],
        invitee_name: str,
        amount: float,
        gift_name: str
    ):
        """
        Sends thank you receipt/messages via console logging (stub) or SMTP/SMS gateway.
        """
        message = (
            f"Dear {invitee_name},\n\n"
            f"Thank you for contributing ₹{amount:,.2f} towards '{gift_name}'.\n"
            f"Your contribution has been successfully received!\n\n"
            f"Warm Regards,\nShagun Team"
        )

        # 1. Console / System log
        logger.info(f"--- NOTIFICATION SENT ---")
        logger.info(f"To Phone: {to_phone}")
        logger.info(f"To Email: {to_email or 'N/A'}")
        logger.info(f"Content:\n{message}")
        logger.info(f"-------------------------")

        # 2. Add email/SMS gateway logic here if credentials are set
        # Since SMTP settings are optional, we wrap it in a try-catch and only send if host/user is filled
        from app.config import settings
        if settings.SMTP_USER and to_email:
            try:
                import smtplib
                from email.mime.text import MIMEText
                from email.mime.multipart import MIMEMultipart

                msg = MIMEMultipart()
                msg['From'] = settings.FROM_EMAIL
                msg['To'] = to_email
                msg['Subject'] = f"Thank you for your Shagun contribution to {gift_name}!"
                msg.attach(MIMEText(message, 'plain'))

                with smtplib.SMTP(settings.SMTP_HOST, settings.SMTP_PORT) as server:
                    if settings.SMTP_PORT == 587:
                        server.starttls()
                    server.login(settings.SMTP_USER, settings.SMTP_PASS)
                    server.send_message(msg)
                logger.info(f"Email notification successfully sent to {to_email}")
            except Exception as e:
                logger.error(f"Failed to send email: {e}")

notification_service = NotificationService()
