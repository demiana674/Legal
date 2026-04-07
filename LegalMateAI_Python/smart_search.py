"""
Smart Search with FAISS and Semantic Search
"""
import json
import os
import numpy as np
import logging
from typing import List, Dict, Any

logger = logging.getLogger(__name__)

class SmartSearch:
    """
    Semantic search using FAISS and sentence transformers
    """
    def __init__(self, auto_load_path: str = None, index_path: str = "faiss.index", 
                 docs_path: str = "documents.json"):
        self.model = None
        self.index = None
        self.documents = []
        self.is_loaded = False
        self.index_path = index_path
        self.docs_path = docs_path
        
        # Load embedding model
        try:
            from sentence_transformers import SentenceTransformer
            self.model = SentenceTransformer('paraphrase-multilingual-MiniLM-L12-v2')
            logger.info("SentenceTransformer loaded successfully")
        except Exception as e:
            logger.error(f"Failed to load SentenceTransformer: {e}")
            self.model = None
        
        # Load existing index if available
        if os.path.exists(index_path) and os.path.exists(docs_path):
            self.load_index(index_path)
            self._load_documents()
            logger.info(f"Loaded existing index with {len(self.documents)} documents")
        elif auto_load_path and os.path.exists(auto_load_path):
            self.load_from_json(auto_load_path)
    
    def _load_documents(self):
        """Load documents from disk"""
        try:
            if os.path.exists(self.docs_path):
                with open(self.docs_path, 'r', encoding='utf-8') as f:
                    self.documents = json.load(f)
                logger.info(f"Loaded {len(self.documents)} documents from {self.docs_path}")
        except Exception as e:
            logger.error(f"Failed to load documents: {e}")
    
    def _save_documents(self):
        """Save documents to disk"""
        try:
            with open(self.docs_path, 'w', encoding='utf-8') as f:
                json.dump(self.documents, f, ensure_ascii=False, indent=2)
            logger.info(f"Saved {len(self.documents)} documents to {self.docs_path}")
        except Exception as e:
            logger.error(f"Failed to save documents: {e}")
    
    def build_index(self, documents: List[str]):
        """Build FAISS index from documents"""
        if self.model is None:
            logger.warning("No embedding model available")
            return
        
        try:
            import faiss
            
            self.documents = documents
            
            if not documents:
                logger.warning("No documents to build index")
                return
            
            logger.info(f"Building index for {len(documents)} documents...")
            embeddings = self.model.encode(documents)
            
            dim = embeddings.shape[1]
            self.index = faiss.IndexFlatL2(dim)
            self.index.add(embeddings.astype(np.float32))
            
            self.is_loaded = True
            logger.info(f"Index built successfully with {len(documents)} documents")
            
            # Save index and documents
            self.save_index()
            self._save_documents()
            
        except Exception as e:
            logger.error(f"Failed to build index: {e}")
            self.is_loaded = False
    
    def save_index(self, path: str = None):
        """Save FAISS index to disk"""
        path = path or self.index_path
        try:
            import faiss
            if self.index:
                faiss.write_index(self.index, path)
                logger.info(f"Index saved to {path}")
        except Exception as e:
            logger.error(f"Failed to save index: {e}")
    
    def load_index(self, path: str = None):
        """Load FAISS index from disk"""
        path = path or self.index_path
        try:
            import faiss
            if os.path.exists(path):
                self.index = faiss.read_index(path)
                self.is_loaded = True
                logger.info(f"Index loaded from {path}")
        except Exception as e:
            logger.error(f"Failed to load index: {e}")
    
    def search(self, query: str, k: int = 5) -> List[Dict[str, Any]]:
        """Search for similar documents"""
        logger.info(f"Search - Query: {query[:50]}..., Limit: {k}")
        
        if not self.is_loaded or self.model is None:
            logger.warning("Search not available")
            return self._fallback_search(query, k)
        
        try:
            import faiss
            
            query_emb = self.model.encode([query])
            distances, indices = self.index.search(query_emb.astype(np.float32), k)
            
            results = []
            for idx, dist in zip(indices[0], distances[0]):
                if idx < len(self.documents):
                    similarity = 1 / (1 + dist)
                    results.append({
                        "content": self.documents[idx][:500],
                        "score": round(similarity, 3),
                        "index": int(idx)
                    })
            
            logger.info(f"Search results - Found: {len(results)}")
            return results
            
        except Exception as e:
            logger.error(f"Search error: {e}")
            return self._fallback_search(query, k)
    
    def _fallback_search(self, query: str, k: int = 5) -> List[Dict[str, Any]]:
        """Fallback keyword-based search"""
        query_lower = query.lower()
        results = []
        
        for idx, doc in enumerate(self.documents):
            if query_lower in doc.lower():
                results.append({
                    "content": doc[:500],
                    "score": 0.5,
                    "index": idx
                })
        
        return results[:k]
    
    def load_from_json(self, file_path: str):
        """Load documents from JSON and build index"""
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                data = json.load(f)
            
            documents = []
            for item in data:
                if isinstance(item, dict):
                    content = item.get("Content") or item.get("content") or item.get("text")
                    if content:
                        documents.append(content)
                elif isinstance(item, str):
                    documents.append(item)
            
            if documents:
                self.build_index(documents)
                logger.info(f"Loaded {len(documents)} documents from {file_path}")
            else:
                logger.warning(f"No valid documents found in {file_path}")
                
        except Exception as e:
            logger.error(f"Failed to load from JSON: {e}")
    
    def add_document(self, document: str):
        """Add document to index incrementally"""
        if not self.is_loaded:
            self.build_index([document])
            return
        
        try:
            import faiss
            emb = self.model.encode([document])
            self.index.add(emb.astype(np.float32))
            self.documents.append(document)
            logger.info(f"Added document, total: {len(self.documents)}")
            
            self.save_index()
            self._save_documents()
            
        except Exception as e:
            logger.error(f"Failed to add document: {e}")