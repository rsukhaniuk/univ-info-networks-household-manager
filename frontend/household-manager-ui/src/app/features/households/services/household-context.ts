import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface HouseholdContextData {
  id: string;
  name: string;
  isOwner: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class HouseholdContext {
  private currentHouseholdSubject = new BehaviorSubject<HouseholdContextData | null>(null);
  public currentHousehold$: Observable<HouseholdContextData | null> = this.currentHouseholdSubject.asObservable();

  setHousehold(household: HouseholdContextData | null): void {
    this.currentHouseholdSubject.next(household);
  }

  clearHousehold(): void {
    this.currentHouseholdSubject.next(null);
  }

  getCurrentHousehold(): HouseholdContextData | null {
    return this.currentHouseholdSubject.value;
  }
}
