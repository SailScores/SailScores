import { Guid } from "../guid";

export interface competitorDto {
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
