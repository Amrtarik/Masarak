import { TestBed } from '@angular/core/testing';

import { ChatService } from './signalr';

describe('Signalr', () => {
  let service: ChatService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ChatService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
