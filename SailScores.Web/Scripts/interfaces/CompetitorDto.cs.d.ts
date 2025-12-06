export interface competitorDto {
	id: string;
	clubId: string;
	name: string;
	sailNumber: string;
	alternativeSailNumber?: string;
	boatName: string;
    homeClubName: string;
	notes?: string;
    isActive?: boolean;
    boatClassId: string;
	fleetIds: string[];
	scoreIds: string[];
}
