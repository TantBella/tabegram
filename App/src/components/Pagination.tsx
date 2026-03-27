import React from 'react';
import { Pagination as BSPagination } from 'react-bootstrap';

interface PaginationProps {
  current: number;
  total: number;
  pageSize: number;
  onChange: (page: number) => void;
}

export const Pagination: React.FC<PaginationProps> = ({ current, total, pageSize, onChange }) => {
  const pages = Math.ceil(total / pageSize);

  if (pages <= 1) return null;

  const items = [];
  const start = Math.max(1, current - 2);
  const end = Math.min(pages, current + 2);

  if (start > 1) {
    items.push(
      <BSPagination.Item key={1} onClick={() => onChange(1)}>
        1
      </BSPagination.Item>
    );
    if (start > 2) items.push(<BSPagination.Ellipsis key="start" disabled />);
  }

  for (let i = start; i <= end; i++) {
    items.push(
      <BSPagination.Item
        key={i}
        active={i === current}
        onClick={() => onChange(i)}
      >
        {i}
      </BSPagination.Item>
    );
  }

  if (end < pages) {
    if (end < pages - 1) items.push(<BSPagination.Ellipsis key="end" disabled />);
    items.push(
      <BSPagination.Item key={pages} onClick={() => onChange(pages)}>
        {pages}
      </BSPagination.Item>
    );
  }

  return (
    <div className="d-flex justify-content-center mt-4">
      <BSPagination>{items}</BSPagination>
    </div>
  );
};
