export enum UserRole { Admin = 'Admin', Employee = 'Employee', Manager = 'Manager', Finance = 'Finance' }
export enum DepartmentType { HR = 'HR', IT = 'IT', Sales = 'Sales', Marketing = 'Marketing', Finance = 'Finance' }
export enum UserStatus { Active = 'Active', Inactive = 'Inactive', Suspended = 'Suspended' }
export enum ApprovalLevel { Level1 = 'Level1', Level2 = 'Level2', Finance = 'Finance' }
export class CreateUserResponseDto { userId: string = ''; userName: string = ''; role: UserRole = UserRole.Employee; department: DepartmentType = DepartmentType.IT; status: UserStatus = UserStatus.Active; approvalLevel?: ApprovalLevel; }
export function enumToArray(enumObject: any): string[] { return Object.keys(enumObject).filter(k => isNaN(Number(k))).map(k => enumObject[k]); }
