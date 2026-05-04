export interface PaginatedResult<T> {
    data: T[];
    pageNumber: number;
    pageSize: number;
    totalCount: number;
}