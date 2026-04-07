"""
API Key Repository - Manages API keys with expiry and permissions
"""
import json
import os
import secrets
import logging
from typing import Dict, List, Optional
from datetime import datetime, timedelta
from threading import Lock

logger = logging.getLogger(__name__)


class APIKeyEntity:
    """API Key entity"""
    
    def __init__(self, key: str, name: str, permissions: List[str],
                 created_at: datetime, expires_at: Optional[datetime],
                 rate_limit: int, is_active: bool, usage_count: int,
                 last_used: Optional[datetime]):
        self.key = key
        self.name = name
        self.permissions = permissions
        self.created_at = created_at
        self.expires_at = expires_at
        self.rate_limit = rate_limit
        self.is_active = is_active
        self.usage_count = usage_count
        self.last_used = last_used
    
    def to_dict(self) -> Dict:
        return {
            "key": self.key,
            "name": self.name,
            "permissions": self.permissions,
            "created_at": self.created_at.isoformat(),
            "expires_at": self.expires_at.isoformat() if self.expires_at else None,
            "rate_limit": self.rate_limit,
            "is_active": self.is_active,
            "usage_count": self.usage_count,
            "last_used": self.last_used.isoformat() if self.last_used else None
        }
    
    @classmethod
    def from_dict(cls, data: Dict) -> "APIKeyEntity":
        return cls(
            key=data["key"],
            name=data["name"],
            permissions=data.get("permissions", ["analyze", "chat", "search"]),
            created_at=datetime.fromisoformat(data["created_at"]),
            expires_at=datetime.fromisoformat(data["expires_at"]) if data.get("expires_at") else None,
            rate_limit=data.get("rate_limit", 100),
            is_active=data.get("is_active", True),
            usage_count=data.get("usage_count", 0),
            last_used=datetime.fromisoformat(data["last_used"]) if data.get("last_used") else None
        )
    
    def is_valid(self, permission: Optional[str] = None) -> bool:
        """Check if key is valid"""
        if not self.is_active:
            return False
        if self.expires_at and datetime.now() > self.expires_at:
            return False
        if permission and permission not in self.permissions:
            return False
        return True


class APIKeyRepository:
    """
    Repository for API keys with persistence
    """
    
    def __init__(self, storage_path: str = "api_keys.json"):
        self.storage_path = storage_path
        self._keys: Dict[str, APIKeyEntity] = {}
        self._lock = Lock()
        self._load()
    
    def _load(self):
        """Load from disk"""
        if os.path.exists(self.storage_path):
            try:
                with open(self.storage_path, 'r', encoding='utf-8') as f:
                    data = json.load(f)
                    for key_data in data:
                        entity = APIKeyEntity.from_dict(key_data)
                        self._keys[entity.key] = entity
                logger.info(f"Loaded {len(self._keys)} API keys")
            except Exception as e:
                logger.error(f"Failed to load API keys: {e}")
    
    def _save(self):
        """Save to disk"""
        try:
            with open(self.storage_path, 'w', encoding='utf-8') as f:
                data = [key.to_dict() for key in self._keys.values()]
                json.dump(data, f, ensure_ascii=False, indent=2)
        except Exception as e:
            logger.error(f"Failed to save API keys: {e}")
    
    def create(self, name: str, permissions: List[str],
               expires_in_days: int = 90, rate_limit: int = 100) -> str:
        """Create new API key"""
        with self._lock:
            key = f"lm_{secrets.token_urlsafe(32)}"
            
            expires_at = datetime.now() + timedelta(days=expires_in_days)
            
            entity = APIKeyEntity(
                key=key,
                name=name,
                permissions=permissions,
                created_at=datetime.now(),
                expires_at=expires_at,
                rate_limit=rate_limit,
                is_active=True,
                usage_count=0,
                last_used=None
            )
            
            self._keys[key] = entity
            self._save()
            logger.info(f"Created API key: {name}")
            return key
    
    def validate(self, key: str, permission: Optional[str] = None) -> Optional[APIKeyEntity]:
        """Validate API key and update usage"""
        with self._lock:
            entity = self._keys.get(key)
            
            if not entity or not entity.is_valid(permission):
                return None
            
            # Update usage stats
            updated_entity = APIKeyEntity(
                key=entity.key,
                name=entity.name,
                permissions=entity.permissions,
                created_at=entity.created_at,
                expires_at=entity.expires_at,
                rate_limit=entity.rate_limit,
                is_active=entity.is_active,
                usage_count=entity.usage_count + 1,
                last_used=datetime.now()
            )
            self._keys[key] = updated_entity
            self._save()
            
            return updated_entity
    
    def revoke(self, key: str) -> bool:
        """Revoke API key"""
        with self._lock:
            if key in self._keys:
                entity = self._keys[key]
                updated = APIKeyEntity(
                    key=entity.key,
                    name=entity.name,
                    permissions=entity.permissions,
                    created_at=entity.created_at,
                    expires_at=entity.expires_at,
                    rate_limit=entity.rate_limit,
                    is_active=False,
                    usage_count=entity.usage_count,
                    last_used=entity.last_used
                )
                self._keys[key] = updated
                self._save()
                logger.info(f"Revoked API key: {entity.name}")
                return True
            return False
    
    def list_all(self) -> List[Dict]:
        """List all keys (safe preview)"""
        with self._lock:
            return [
                {
                    "name": entity.name,
                    "key_preview": entity.key[:12] + "...",
                    "permissions": entity.permissions,
                    "expires_at": entity.expires_at.isoformat() if entity.expires_at else None,
                    "is_active": entity.is_active,
                    "usage_count": entity.usage_count,
                    "last_used": entity.last_used.isoformat() if entity.last_used else None
                }
                for entity in self._keys.values()
            ]