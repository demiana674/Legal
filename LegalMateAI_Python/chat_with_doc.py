"""
Chat with Document - RAG-based chat with documents
"""
import json
import logging
import numpy as np
from typing import List, Dict, Any

logger = logging.getLogger(__name__)

class ChatWithDocument:
    """
    RAG-based chat with documents using semantic search
    """
    def __init__(self, model):
        self.model = model
        self.embeddings_model = None
        
        try:
            from sentence_transformers import SentenceTransformer
            self.embeddings_model = SentenceTransformer('paraphrase-multilingual-MiniLM-L12-v2')
            logger.info("Embeddings model loaded for RAG")
        except Exception as e:
            logger.warning(f"Could not load embeddings model: {e}")
    
    def ask(self, text: str, question: str) -> Dict[str, Any]:
        """
        Answer question based on document using RAG
        """
        logger.info("Processing chat request")
        
        # 1. Retrieve relevant chunks
        relevant_chunks = self._retrieve_relevant_chunks(text, question)
        
        # 2. Build context from top chunks
        context = "\n\n".join([c["content"] for c in relevant_chunks[:3]])
        
        # 3. Calculate confidence
        confidence = self._calculate_confidence(relevant_chunks)
        
        # 4. Generate answer
        if self.model and self.model._generator:
            prompt = f"""
            بناءً على النص القانوني التالي:

            {context[:2000]}

            السؤال: {question}

            أجب على السؤال بشكل دقيق ومختصر. إذا لم تجد الإجابة في النص، اذكر ذلك بوضوح.
            """
            
            answer = self.model.generate(prompt, max_tokens=500)
        else:
            answer = self._fallback_answer(question, context)
        
        logger.info(f"Chat completed - Confidence: {confidence}, Chunks: {len(relevant_chunks)}")
        
        return {
            "answer": answer.strip(),
            "relevant_context": context[:500],
            "confidence": confidence,
            "retrieved_chunks_count": len(relevant_chunks),
            "top_score": relevant_chunks[0]["score"] if relevant_chunks else 0
        }
    
    def _retrieve_relevant_chunks(self, text: str, question: str, k: int = 3) -> List[Dict[str, Any]]:
        """Retrieve relevant chunks using embeddings"""
        if self.embeddings_model is None:
            return self._keyword_retrieval(text, question, k)
        
        try:
            # Chunk text
            chunks = self._chunk_text(text)
            
            if not chunks:
                return []
            
            # Generate embeddings
            question_emb = self.embeddings_model.encode([question])
            chunk_embs = self.embeddings_model.encode(chunks)
            
            # Calculate cosine similarity
            from sklearn.metrics.pairwise import cosine_similarity
            similarities = cosine_similarity(chunk_embs, question_emb).flatten()
            
            # Get top k
            top_indices = np.argsort(similarities)[-k:][::-1]
            
            results = []
            for idx in top_indices:
                results.append({
                    "content": chunks[idx],
                    "score": float(similarities[idx]),
                    "position": idx
                })
            
            logger.info(f"Retrieved {len(results)} chunks, top score: {results[0]['score']:.3f}")
            return results
            
        except Exception as e:
            logger.error(f"Embedding retrieval failed: {e}")
            return self._keyword_retrieval(text, question, k)
    
    def _keyword_retrieval(self, text: str, question: str, k: int = 3) -> List[Dict[str, Any]]:
        """Fallback keyword-based retrieval"""
        question_lower = question.lower()
        keywords = [w for w in question_lower.split() if len(w) > 3]
        
        chunks = self._chunk_text(text)
        
        scored_chunks = []
        for idx, chunk in enumerate(chunks):
            chunk_lower = chunk.lower()
            score = sum(1 for kw in keywords if kw in chunk_lower)
            if score > 0:
                scored_chunks.append({
                    "content": chunk,
                    "score": score / len(keywords) if keywords else 0,
                    "position": idx
                })
        
        scored_chunks.sort(key=lambda x: x["score"], reverse=True)
        return scored_chunks[:k]
    
    def _chunk_text(self, text: str, chunk_size: int = 500, overlap: int = 50) -> List[str]:
        """Chunk text with overlap"""
        words = text.split()
        chunks = []
        
        if len(words) <= chunk_size:
            return [text]
        
        start = 0
        while start < len(words):
            end = start + chunk_size
            chunk = " ".join(words[start:end])
            chunks.append(chunk)
            start += (chunk_size - overlap)
        
        return chunks
    
    def _calculate_confidence(self, chunks: List[Dict[str, Any]]) -> str:
        """Calculate confidence based on retrieval scores"""
        if not chunks:
            return "منخفضة"
        
        avg_score = sum(c["score"] for c in chunks) / len(chunks)
        
        if avg_score > 0.7:
            return "عالية"
        elif avg_score > 0.4:
            return "متوسطة"
        else:
            return "منخفضة"
    
    def _fallback_answer(self, question: str, context: str) -> str:
        """Fallback answer"""
        if context:
            return f"بناءً على النص المتاح: {context[:300]}..."
        else:
            return "لم أجد معلومات كافية للإجابة على سؤالك. يُنصح باستشارة محامٍ متخصص."