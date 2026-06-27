import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PlansComponent as PlansPageComponentBase } from '../../components/plans-component/plans-component';

@Component({
  selector: 'app-plans-page',
  standalone: true,
  imports: [CommonModule, PlansPageComponentBase],
  template: `<plans-component></plans-component>`,
})
export class PlansPageComponent {}
