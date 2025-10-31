import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ServerErrorService {
  private errorsSubject = new BehaviorSubject<string[] | null>(null);
  errors$ = this.errorsSubject.asObservable();

  setErrors(errors: string[] | null) {
    this.errorsSubject.next(errors && errors.length ? errors : null);
  }

  clear() {
    this.errorsSubject.next(null);
  }
}
