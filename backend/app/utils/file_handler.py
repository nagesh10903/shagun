import os
import uuid
from fastapi import UploadFile, HTTPException
from app.config import settings

def save_uploaded_file(file: UploadFile, subfolder: str = "gifts") -> str:
    """
    Saves an uploaded file to the local storage, returning its relative path/url.
    Validates file size and basic image types.
    """
    # 1. Validate file extension
    allowed_extensions = {".jpg", ".jpeg", ".png", ".webp"}
    filename = file.filename
    _, ext = os.path.splitext(filename.lower())
    if ext not in allowed_extensions:
        raise HTTPException(
            status_code=400,
            detail=f"Invalid file format. Supported formats: {', '.join(allowed_extensions)}"
        )

    # 2. Check directory exists
    target_dir = os.path.join(settings.UPLOAD_DIR, subfolder)
    os.makedirs(target_dir, exist_ok=True)

    # 3. Generate unique filename
    unique_filename = f"{uuid.uuid4().hex}{ext}"
    file_path = os.path.join(target_dir, unique_filename)

    # 4. Save file
    try:
        with open(file_path, "wb") as buffer:
            # We can write chunk by chunk to avoid reading entire file into memory
            # Max upload size is checked after reading or chunk by chunk
            size = 0
            max_size = settings.MAX_UPLOAD_SIZE_MB * 1024 * 1024
            while True:
                chunk = file.file.read(1024 * 1024)  # 1MB chunk
                if not chunk:
                    break
                size += len(chunk)
                if size > max_size:
                    raise HTTPException(
                        status_code=400,
                        detail=f"File exceeds maximum upload size of {settings.MAX_UPLOAD_SIZE_MB}MB"
                    )
                buffer.write(chunk)
    except HTTPException:
        # Re-raise size exception
        if os.path.exists(file_path):
            os.remove(file_path)
        raise
    except Exception as e:
        if os.path.exists(file_path):
            os.remove(file_path)
        raise HTTPException(status_code=500, detail=f"Failed to save file: {str(e)}")

    # Return relative URL path
    return f"/{settings.UPLOAD_DIR}/{subfolder}/{unique_filename}"
