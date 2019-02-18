import { Guid } from "../guid";

export interface scoreCodeDto {
    id: Guid;
    clubId: Guid;
	text: string;
	description: string;
	countAsCompetitor?: boolean;
	discardable?: boolean;
	useAverageResult?: boolean;
	competitorCountPlus?: number;
}
