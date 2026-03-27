import React, { useState, useEffect } from 'react';
import { Container, Row, Col, Spinner, Alert } from 'react-bootstrap';
import { postsApi } from '../api/posts';
import type { Post } from '../types';
import { PostCard } from '../components/PostCard';
import { Pagination } from '../components/Pagination';

export const FeedPage: React.FC = () => {
  const [posts, setPosts] = useState<Post[]>([]);
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [pageSize] = useState(10);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    fetchPosts();
  }, [page]);

  const fetchPosts = async () => {
    setLoading(true);
    setError('');
    try {
      const res = await postsApi.getPosts(page, pageSize);
      setPosts(res.data);
      setTotal(res.total);
    } catch (err) {
      setError('Failed to load posts');
    } finally {
      setLoading(false);
    }
  };

  const handleLike = (postId: string) => {
    setPosts(posts.map(p =>
      p.id === postId
        ? {
          ...p,
          likedByCurrentUser: !p.likedByCurrentUser,
          likeCount: p.likedByCurrentUser ? p.likeCount - 1 : p.likeCount + 1
        }
        : p
    ));
  };

  return (
    <Container className="mt-4">
      {error && <Alert variant="danger">{error}</Alert>}
      {loading && <Spinner animation="border" />}
      <Row>
        <Col md={8} className="mx-auto">
          {posts.map(post => (
            <PostCard key={post.id} post={post} onLike={handleLike} />
          ))}
          {!loading && posts.length === 0 && <p>No posts yet</p>}
        </Col>
      </Row>
      {total > pageSize && (
        <Pagination
          current={page}
          total={total}
          pageSize={pageSize}
          onChange={setPage}
        />
      )}
    </Container>
  );
};
