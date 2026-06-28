import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CheckoutCancelComponent as CheckoutCancelPageComponentBase } from '../../components/checkout-cancel-component/checkout-cancel-component';

@Component({
  selector: 'app-checkout-cancel-page',
  standalone: true,
  imports: [CommonModule, CheckoutCancelPageComponentBase],
  template: `<app-checkout-cancel></app-checkout-cancel>`,
})
export class CheckoutCancelPageComponent {}
