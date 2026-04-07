"""
Timeout Utilities - Cross-platform timeout handling
"""
import asyncio
import functools
import logging
import concurrent.futures
from typing import Any, Callable

logger = logging.getLogger(__name__)


def async_timeout(seconds: int):
    """Async timeout decorator using asyncio.wait_for"""
    def decorator(func: Callable) -> Callable:
        @functools.wraps(func)
        async def wrapper(*args, **kwargs) -> Any:
            try:
                return await asyncio.wait_for(
                    func(*args, **kwargs),
                    timeout=seconds
                )
            except asyncio.TimeoutError:
                logger.error(f"Timeout after {seconds}s in {func.__name__}")
                raise TimeoutError(f"Operation timed out after {seconds} seconds")
        return wrapper
    return decorator


def sync_timeout(seconds: int):
    """Sync timeout using ThreadPoolExecutor"""
    def decorator(func: Callable) -> Callable:
        @functools.wraps(func)
        def wrapper(*args, **kwargs) -> Any:
            with concurrent.futures.ThreadPoolExecutor(max_workers=1) as executor:
                future = executor.submit(func, *args, **kwargs)
                try:
                    return future.result(timeout=seconds)
                except concurrent.futures.TimeoutError:
                    logger.error(f"Timeout after {seconds}s in {func.__name__}")
                    raise TimeoutError(f"Operation timed out after {seconds} seconds")
        return wrapper
    return decorator


class TimeoutContext:
    """Context manager for timeout"""
    
    def __init__(self, seconds: int, operation: str = "operation"):
        self.seconds = seconds
        self.operation = operation
        self.executor = None
        self.future = None
    
    def __enter__(self):
        self.executor = concurrent.futures.ThreadPoolExecutor(max_workers=1)
        return self
    
    def __exit__(self, exc_type, exc_val, exc_tb):
        if self.executor:
            self.executor.shutdown(wait=False)
    
    def execute(self, func: Callable, *args, **kwargs):
        """Execute function with timeout"""
        self.future = self.executor.submit(func, *args, **kwargs)
        try:
            return self.future.result(timeout=self.seconds)
        except concurrent.futures.TimeoutError:
            logger.error(f"Timeout after {self.seconds}s in {self.operation}")
            raise TimeoutError(f"{self.operation} timed out after {self.seconds} seconds")