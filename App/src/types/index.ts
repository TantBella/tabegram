export interface User {
  id: string;
  username: string;
}

export interface Post {
  id: string;
  userId: string;
  username: string;
  imageUrl: string;
  description?: string;
  createdAt: string;
  likeCount: number;
  likedByCurrentUser: boolean;
}

export interface AuthResponse {
  userId: string;
  username: string;
  token: string;
}

export interface CreatePostRequest {
  image: File;
  description?: string;
}

export interface PagedResponse<T> {
  data: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface UserPostDto {
  id: string;
  imageUrl: string;
  description?: string;
  createdAt: string;
  likeCount: number;
}

export interface UserPostsResponse {
  userId: string;
  username: string;
  posts: UserPostDto[];
}
