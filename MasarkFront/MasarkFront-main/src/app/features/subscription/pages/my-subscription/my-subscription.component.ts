import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MySubscriptionComponent as MySubscriptionPageComponentBase } from '../../components/my-subscription-component/my-subscription-component';

@Component({
  selector: 'app-my-subscription-page',
  standalone: true,
  imports: [CommonModule, MySubscriptionPageComponentBase],
  template: `<my-subscription-component></my-subscription-component>`,
})
export class MySubscriptionPageComponent {}
