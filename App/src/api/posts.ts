import client from './client';
import type { Post, PagedResponse, UserPostsResponse } from '../types';

export const postsApi = {
  getPosts: async (page: number = 1, pageSize: number = 10): Promise<PagedResponse<Post>> => {
    const res = await client.get('/posts', { params: { page, pageSize } });
    return res.data;
  },

  createPost: async (image: File, description?: string): Promise<{ postId: string }> => {
    const formData = new FormData();
    formData.append('Image', image);
    if (description) formData.append('Description', description);
    
    const res = await client.post('/posts', formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    });
    return res.data;
  },

  toggleLike: async (postId: string): Promise<void> => {
    await client.post(`/posts/${postId}/like`);
  },

  getUserPosts: async (userId: string): Promise<UserPostsResponse> => {
    const res = await client.get(`/users/${userId}/posts`);
    return res.data;
  }
};
