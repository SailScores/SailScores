import { Guid } from "../guid";

export interface scoreCodeDto {
    id: Guid;
    clubId: Guid;
	name: string;
	description: string;
	formula: string;
	formulaValue?: number;
	scoreLike: string;
	discardable?: boolean;
	cameToStart?: boolean;
	started?: boolean;
	finished?: boolean;
	preserveResult?: boolean;
	/** Should scoring of other following competitors use this as a finisher ahead? */
	adjustOtherScores?: boolean;
}

