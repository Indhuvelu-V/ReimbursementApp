<<<<<<< HEAD
export enum UserRole { Admin = 'Admin', Employee = 'Employee', Manager = 'Manager', Finance = 'Finance' }
export enum DepartmentType { HR = 'HR', IT = 'IT', Sales = 'Sales', FinanceAnalyst = 'FinanceAnalyst' }
=======
export enum UserRole { Admin = 'Admin', Employee = 'Employee', TeamLead = 'TeamLead', Manager = 'Manager', Finance = 'Finance' }
export enum DepartmentType { HR = 'HR', IT = 'IT', Sales = 'Sales', Marketing = 'Marketing', Finance = 'Finance' }
>>>>>>> eba5464 (Feature added)
export enum UserStatus { Active = 'Active', Inactive = 'Inactive', Suspended = 'Suspended' }
export enum ApprovalLevel { Level1 = 'Level1', Level2 = 'Level2', Finance = 'Finance' }
export class CreateUserResponseDto {
  userId: string = '';
  userName: string = '';
  email: string = '';
  role: UserRole = UserRole.Employee;
  department: DepartmentType = DepartmentType.IT;
  status: UserStatus = UserStatus.Active;
  approvalLevel?: ApprovalLevel;
  reportingManagerId?: string;
  reportingManagerName?: string;
}
export function enumToArray(enumObject: any): string[] { return Object.keys(enumObject).
    filter(k => isNaN(Number(k))).map(k => enumObject[k]); }
