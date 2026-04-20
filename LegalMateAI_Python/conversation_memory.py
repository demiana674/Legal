"""
Conversation Memory - Manages chat history with TTL and persistence
"""
import json
import os
import hashlib
import logging
from typing import List, Dict, Any, Optional
from datetime import datetime, timedelta
from collections import OrderedDict
from threading import Lock

logger = logging.getLogger(__name__)


class ConversationMemory:
    """
    Manages conversation history with TTL (Time-To-Live)
    Similar to ChatGPT's conversation memory
    """
    
    def __init__(self, max_history: int = 20, ttl_minutes: int = 60, 
                 persistence_path: str = "conversations.json"):
        self.max_history = max_history  # أقصى عدد للرسائل في المحادثة
        self.ttl_minutes = ttl_minutes  # مدة صلاحية المحادثة
        self.persistence_path = persistence_path
        self._conversations: Dict[str, Dict[str, Any]] = OrderedDict()
        self._lock = Lock()
        
        # تحميل المحادثات السابقة
        self._load_from_disk()
    
    def _get_conversation_key(self, user_id: str, session_id: Optional[str] = None) -> str:
        """إنشاء مفتاح فريد للمحادثة"""
        if session_id:
            return f"{user_id}:{session_id}"
        return user_id
    
    def get_history(self, user_id: str, session_id: Optional[str] = None) -> List[Dict[str, str]]:
        """
        جلب تاريخ المحادثة لمستخدم معين
        """
        key = self._get_conversation_key(user_id, session_id)
        
        with self._lock:
            if key not in self._conversations:
                return []
            
            conv = self._conversations[key]
            
            # التحقق من صلاحية المحادثة
            if self._is_expired(conv):
                del self._conversations[key]
                return []
            
            # تحديث وقت آخر نشاط
            conv["last_activity"] = datetime.now()
            
            # نقل للمقدمة (LRU)
            self._conversations.move_to_end(key)
            
            return conv.get("messages", [])
    
    def add_message(self, user_id: str, role: str, content: str, 
                    session_id: Optional[str] = None):
        """
        إضافة رسالة جديدة للمحادثة
        """
        key = self._get_conversation_key(user_id, session_id)
        
        with self._lock:
            if key not in self._conversations:
                self._conversations[key] = {
                    "user_id": user_id,
                    "session_id": session_id,
                    "messages": [],
                    "created_at": datetime.now(),
                    "last_activity": datetime.now()
                }
            
            conv = self._conversations[key]
            
            # إضافة الرسالة
            conv["messages"].append({
                "role": role,
                "content": content,
                "timestamp": datetime.now().isoformat()
            })
            
            # حذف الرسائل القديمة إذا تجاوزت الحد
            if len(conv["messages"]) > self.max_history:
                conv["messages"] = conv["messages"][-self.max_history:]
            
            conv["last_activity"] = datetime.now()
            
            # نقل للمقدمة
            self._conversations.move_to_end(key)
            
            # تنظيف المحادثات منتهية الصلاحية
            self._cleanup_expired()
            
            # حفظ دوري
            if len(self._conversations) % 10 == 0:
                self._save_to_disk()
    
    def clear_history(self, user_id: str, session_id: Optional[str] = None):
        """
        مسح تاريخ المحادثة
        """
        key = self._get_conversation_key(user_id, session_id)
        
        with self._lock:
            if key in self._conversations:
                del self._conversations[key]
                logger.info(f"Cleared conversation for {key}")
    
    def _is_expired(self, conv: Dict[str, Any]) -> bool:
        """التحقق من انتهاء صلاحية المحادثة"""
        last_activity = conv.get("last_activity")
        if not last_activity:
            return True
        
        expiry_time = last_activity + timedelta(minutes=self.ttl_minutes)
        return datetime.now() > expiry_time
    
    def _cleanup_expired(self):
        """حذف المحادثات منتهية الصلاحية"""
        expired_keys = []
        
        for key, conv in self._conversations.items():
            if self._is_expired(conv):
                expired_keys.append(key)
        
        for key in expired_keys:
            del self._conversations[key]
        
        if expired_keys:
            logger.info(f"Cleaned up {len(expired_keys)} expired conversations")
    
    def _save_to_disk(self):
        """حفظ المحادثات على القرص"""
        if not self.persistence_path:
            return
        
        try:
            # تحويل التواريخ إلى نصوص
            serializable = {}
            for key, conv in self._conversations.items():
                conv_copy = conv.copy()
                conv_copy["created_at"] = conv["created_at"].isoformat()
                conv_copy["last_activity"] = conv["last_activity"].isoformat()
                serializable[key] = conv_copy
            
            with open(self.persistence_path, 'w', encoding='utf-8') as f:
                json.dump(serializable, f, ensure_ascii=False, indent=2)
            
            logger.debug(f"Saved {len(self._conversations)} conversations to disk")
            
        except Exception as e:
            logger.error(f"Failed to save conversations: {e}")
    
    def _load_from_disk(self):
        """تحميل المحادثات من القرص"""
        if not os.path.exists(self.persistence_path):
            return
        
        try:
            with open(self.persistence_path, 'r', encoding='utf-8') as f:
                data = json.load(f)
            
            loaded_count = 0
            for key, conv in data.items():
                # استعادة التواريخ
                conv["created_at"] = datetime.fromisoformat(conv["created_at"])
                conv["last_activity"] = datetime.fromisoformat(conv["last_activity"])
                
                # التحقق من الصلاحية
                if not self._is_expired(conv):
                    self._conversations[key] = conv
                    loaded_count += 1
            
            logger.info(f"Loaded {loaded_count} conversations from disk")
            
        except Exception as e:
            logger.error(f"Failed to load conversations: {e}")
    
    def get_stats(self) -> Dict[str, Any]:
        """إحصائيات الذاكرة"""
        with self._lock:
            total_messages = sum(len(c.get("messages", [])) for c in self._conversations.values())
            
            return {
                "active_conversations": len(self._conversations),
                "total_messages": total_messages,
                "max_history_per_conversation": self.max_history,
                "ttl_minutes": self.ttl_minutes,
                "persistence_enabled": bool(self.persistence_path)
            }


# Singleton instance
conversation_memory = ConversationMemory()