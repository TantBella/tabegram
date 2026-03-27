import React, { useState } from 'react';
import { Card, Button, Badge } from 'react-bootstrap';
import type { Post } from '../types';
import { postsApi } from '../api/posts';

interface PostCardProps {
  post: Post;
  onLike: (postId: string) => void;
}

export const PostCard: React.FC<PostCardProps> = ({ post, onLike }) => {
  const [likingLoading, setLikingLoading] = useState(false);

  const handleLike = async () => {
    setLikingLoading(true);
    try {
      await postsApi.toggleLike(post.id);
      onLike(post.id);
    } finally {
      setLikingLoading(false);
    }
  };

  return (
    <Card className="mb-3">
      <Card.Img variant="top" src={post.imageUrl} alt="Post" style={{ maxHeight: '400px', objectFit: 'cover' }} />
      <Card.Body>
        <Card.Title className="d-flex justify-content-between align-items-center">
          <a href={`/profile/${post.userId}`} style={{ textDecoration: 'none' }}>
            {post.username}
          </a>
          <small className="text-muted">{new Date(post.createdAt).toLocaleDateString()}</small>
        </Card.Title>
        {post.description && <Card.Text>{post.description}</Card.Text>}
        <div className="d-flex justify-content-between align-items-center">
          <Badge bg="primary">{post.likeCount} likes</Badge>
          <Button
            variant={post.likedByCurrentUser ? 'danger' : 'outline-danger'}
            size="sm"
            onClick={handleLike}
            disabled={likingLoading}
          >
            {post.likedByCurrentUser ? '♥ Unlike' : '♡ Like'}
          </Button>
        </div>
      </Card.Body>
    </Card>
  );
};
