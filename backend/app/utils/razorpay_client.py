import razorpay
from razorpay.errors import SignatureVerificationError
from app.config import settings
import logging

logger = logging.getLogger(__name__)

class RazorpayClient:
    def __init__(self):
        try:
            self.client = razorpay.Client(auth=(settings.RAZORPAY_KEY_ID, settings.RAZORPAY_KEY_SECRET))
        except Exception as e:
            logger.error(f"Failed to initialize Razorpay Client: {e}")
            self.client = None

    def create_order(self, amount_in_paise: int, currency: str = "INR", receipt: str = None) -> dict:
        """
        Creates a Razorpay order. Amount must be in paise (e.g. ₹1 = 100 paise).
        """
        if not self.client:
            raise ValueError("Razorpay client is not initialized")
        
        data = {
            "amount": amount_in_paise,
            "currency": currency,
            "receipt": receipt,
            "payment_capture": 1 # Auto capture
        }
        try:
            order = self.client.order.create(data=data)
            return order
        except Exception as e:
            logger.error(f"Error creating Razorpay order: {str(e)}")
            raise e

    def verify_payment_signature(self, razorpay_order_id: str, razorpay_payment_id: str, razorpay_signature: str) -> bool:
        """
        Verifies the signature sent by the frontend after a payment is made.
        """
        if not self.client:
            return False
            
        params_dict = {
            'razorpay_order_id': razorpay_order_id,
            'razorpay_payment_id': razorpay_payment_id,
            'razorpay_signature': razorpay_signature
        }
        try:
            self.client.utility.verify_payment_signature(params_dict)
            return True
        except SignatureVerificationError:
            logger.warning("Signature verification failed for payment signature check")
            return False
        except Exception as e:
            logger.error(f"Unexpected error verifying payment signature: {str(e)}")
            return False

    def verify_webhook_signature(self, payload: bytes, signature: str, secret: str = None) -> bool:
        """
        Verifies Razorpay Webhook signatures.
        """
        if not self.client:
            return False
        webhook_secret = secret or settings.RAZORPAY_KEY_SECRET # Standard practice is webhook secret, fallback to api key secret
        try:
            self.client.utility.verify_webhook_signature(payload.decode('utf-8'), signature, webhook_secret)
            return True
        except SignatureVerificationError:
            logger.warning("Webhook signature verification failed")
            return False
        except Exception as e:
            logger.error(f"Webhook signature verification error: {str(e)}")
            return False

razorpay_client = RazorpayClient()
