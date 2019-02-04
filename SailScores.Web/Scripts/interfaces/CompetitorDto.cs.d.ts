import { Guid } from "../guid";

declare module server {
	interface competitorDto {
		id: Guid;
		clubId: Guid;
		name: string;
		sailNumber: string;
		alternativeSailNumber?: string;
		boatName: string;
		notes?: string;
		boatClassId: Guid;
		fleetIds: Guid[];
		scoreIds: Guid[];
	}
}
