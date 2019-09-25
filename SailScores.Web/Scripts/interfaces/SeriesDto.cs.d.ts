import { Guid } from "../guid";

export interface seriesDto {
		id: Guid;
		clubId: Guid;
		name: string;
        urlName: string;
		description: string;
		raceIds: Guid[];
		seasonId: Guid;
}
