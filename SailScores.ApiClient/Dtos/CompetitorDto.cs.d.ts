declare module server {
	interface competitorDto {
		id: any;
		clubId: any;
		name: string;
		sailNumber: string;
		alternativeSailNumber: string;
		boatName: string;
		homeClubName: string;
		notes: string;
		isActive: boolean;
		boatClassId: any;
		fleetIds: any[];
		scoreIds: any[];
	}
}
