import { Guid } from "../guid";

export interface competitorDto {
	id: Guid;
	clubId: Guid;
	name: string;
	sailNumber: string;
	alternativeSailNumber?: string;
	boatName: string;
	notes?: string;
    isActive?: boolean;
    boatClassId: Guid;
	fleetIds: Guid[];
	scoreIds: Guid[];
}
