"""
Trace ID Middleware - Adds correlation ID to requests
"""
import uuid
import logging
from starlette.middleware.base import BaseHTTPMiddleware
from starlette.requests import Request
from starlette.responses import Response

logger = logging.getLogger(__name__)

class TraceIDMiddleware(BaseHTTPMiddleware):
    """
    Adds X-Request-ID header and trace_id to logs
    """
    
    async def dispatch(self, request: Request, call_next):
        # Get or generate trace ID
        trace_id = request.headers.get("X-Request-ID")
        if not trace_id:
            trace_id = str(uuid.uuid4())
        
        # Store in request state
        request.state.trace_id = trace_id
        
        # Add to logging context
        old_factory = logging.getLogRecordFactory()
        
        def record_factory(*args, **kwargs):
            record = old_factory(*args, **kwargs)
            record.trace_id = trace_id
            return record
        
        logging.setLogRecordFactory(record_factory)
        
        # Process request
        response: Response = await call_next(request)
        
        # Add trace_id to response
        response.headers["X-Request-ID"] = trace_id
        
        # Restore logging
        logging.setLogRecordFactory(old_factory)
        
        # Log completion
        logger.info(f"Request completed: {request.method} {request.url.path}")
        
        return response


def get_trace_id(request: Request) -> str:
    """Get trace_id from request state"""
    return getattr(request.state, 'trace_id', 'N/A')


class LoggingContext:
    """Context manager for adding trace_id to background tasks"""
    
    def __init__(self, trace_id: str):
        self.trace_id = trace_id
        self.old_factory = None
    
    def __enter__(self):
        self.old_factory = logging.getLogRecordFactory()
        
        def record_factory(*args, **kwargs):
            record = self.old_factory(*args, **kwargs)
            record.trace_id = self.trace_id
            return record
        
        logging.setLogRecordFactory(record_factory)
        return self
    
    def __exit__(self, exc_type, exc_val, exc_tb):
        if self.old_factory:
            logging.setLogRecordFactory(self.old_factory)