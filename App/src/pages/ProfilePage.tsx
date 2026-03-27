import React, { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { Container, Row, Col, Spinner, Alert } from 'react-bootstrap';
import { postsApi } from '../api/posts';
import type { UserPostsResponse } from '../types';

export const ProfilePage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [profile, setProfile] = useState<UserPostsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const fetchProfile = async () => {
      try {
        const res = await postsApi.getUserPosts(id!);
        setProfile(res);
      } catch (err) {
        setError('Failed to load profile');
      } finally {
        setLoading(false);
      }
    };

    if (id) fetchProfile();
  }, [id]);

  if (loading) return <Spinner animation="border" />;
  if (error) return <Alert variant="danger">{error}</Alert>;
  if (!profile) return <Alert variant="warning">Profile not found</Alert>;

  return (
    <Container className="mt-4">
      <h2>{profile.username}'s Posts</h2>
      <Row>
        {profile.posts.map(post => (
          <Col md={4} key={post.id} className="mb-3">
            <div className="card">
              <img src={post.imageUrl} className="card-img-top" alt="Post" />
              <div className="card-body">
                <p className="card-text">{post.description}</p>
                <small className="text-muted">{post.likeCount} likes</small>
              </div>
            </div>
          </Col>
        ))}
      </Row>
      {profile.posts.length === 0 && <p>No posts yet</p>}
    </Container>
  );
};
