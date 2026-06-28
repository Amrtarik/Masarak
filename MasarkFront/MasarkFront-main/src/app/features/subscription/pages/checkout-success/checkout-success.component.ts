import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CheckoutSuccessComponent as CheckoutSuccessPageComponentBase } from '../../components/checkout-success-component/checkout-success-component';

@Component({
  selector: 'app-checkout-success-page',
  standalone: true,
  imports: [CommonModule, CheckoutSuccessPageComponentBase],
  template: `<app-checkout-success></app-checkout-success>`,
})
export class CheckoutSuccessPageComponent {}
