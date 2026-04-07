"""
LRU Cache - True LRU cache with OrderedDict
"""
from collections import OrderedDict
from threading import Lock
import hashlib
import logging
import json
import os
from typing import Any, Dict, Optional
from datetime import datetime

logger = logging.getLogger(__name__)

class LRUCache:
    """
    True LRU Cache using OrderedDict
    Thread-safe with Lock, supports TTL and persistence
    """
    
    def __init__(self, max_size: int = 1000, ttl_seconds: int = 3600,
                 persistence_path: str = None):
        self.max_size = max_size
        self.ttl_seconds = ttl_seconds
        self.persistence_path = persistence_path
        self._cache: OrderedDict = OrderedDict()
        self._lock = Lock()
        self._hits = 0
        self._misses = 0
        
        # Load from disk if persistence enabled
        if persistence_path and os.path.exists(persistence_path):
            self._load_from_disk()
    
    def _hash_key(self, key: str) -> str:
        """Generate hash for cache key"""
        return hashlib.md5(key.encode('utf-8')).hexdigest()
    
    def _is_expired(self, entry: Dict) -> bool:
        """Check if cache entry has expired"""
        if not self.ttl_seconds:
            return False
        created_at = entry.get("created_at", 0)
        return (datetime.now().timestamp() - created_at) > self.ttl_seconds
    
    def _save_to_disk(self):
        """Persist cache to disk"""
        if not self.persistence_path:
            return
        
        try:
            data = []
            for key, entry in self._cache.items():
                data.append({
                    "key": key,
                    "value": entry["value"],
                    "created_at": entry["created_at"]
                })
            
            with open(self.persistence_path, 'w', encoding='utf-8') as f:
                json.dump(data, f, ensure_ascii=False, indent=2)
            
            logger.debug(f"Cache persisted to {self.persistence_path}")
        except Exception as e:
            logger.error(f"Failed to persist cache: {e}")
    
    def _load_from_disk(self):
        """Load cache from disk"""
        try:
            with open(self.persistence_path, 'r', encoding='utf-8') as f:
                data = json.load(f)
            
            for item in data:
                created_at = item.get("created_at", 0)
                if self.ttl_seconds and (datetime.now().timestamp() - created_at) > self.ttl_seconds:
                    continue
                
                self._cache[item["key"]] = {
                    "value": item["value"],
                    "created_at": item["created_at"]
                }
            
            logger.info(f"Loaded {len(self._cache)} entries from cache persistence")
        except Exception as e:
            logger.error(f"Failed to load cache: {e}")
    
    def get(self, key: str) -> Optional[Any]:
        """Get value from cache with LRU update"""
        cache_key = self._hash_key(key)
        
        with self._lock:
            if cache_key not in self._cache:
                self._misses += 1
                return None
            
            entry = self._cache[cache_key]
            
            if self._is_expired(entry):
                del self._cache[cache_key]
                self._misses += 1
                return None
            
            # Move to end (most recently used)
            self._cache.move_to_end(cache_key)
            self._hits += 1
            
            return entry.get("value")
    
    def set(self, key: str, value: Any):
        """Set value in cache with LRU eviction"""
        cache_key = self._hash_key(key)
        
        with self._lock:
            if cache_key in self._cache:
                self._cache.move_to_end(cache_key)
            
            self._cache[cache_key] = {
                "value": value,
                "created_at": datetime.now().timestamp()
            }
            
            # LRU eviction: remove first (oldest) if over size
            while len(self._cache) > self.max_size:
                oldest_key = next(iter(self._cache))
                del self._cache[oldest_key]
                logger.info(f"Cache evicted oldest entry, size now: {len(self._cache)}")
            
            self._save_to_disk()
    
    def clear(self):
        """Clear all cache"""
        with self._lock:
            self._cache.clear()
            self._hits = 0
            self._misses = 0
            if self.persistence_path and os.path.exists(self.persistence_path):
                os.remove(self.persistence_path)
            logger.info("Cache cleared")
    
    def get_stats(self) -> Dict[str, Any]:
        """Get cache statistics"""
        with self._lock:
            total = self._hits + self._misses
            hit_rate = self._hits / total if total > 0 else 0
            return {
                "size": len(self._cache),
                "max_size": self.max_size,
                "ttl_seconds": self.ttl_seconds,
                "hits": self._hits,
                "misses": self._misses,
                "hit_rate": round(hit_rate, 4),
                "utilization_percent": round((len(self._cache) / self.max_size) * 100, 2),
                "persistence_enabled": bool(self.persistence_path)
            }