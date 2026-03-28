export interface CreatePaymentResponseDto {
  paymentId: string; userId: string; userName: string; processedByName?: string;
  amountPaid: number; paymentStatus: string; paymentMode: string;
  referenceNo: string; paymentDate: string; amountInRupees?: string;
  expenseId: string; documentUrls?: string[];
}
