CREATE TABLE "cards" (
	"card_id" UUID NOT NULL,
	"name" VARCHAR(100) NOT NULL,
	"damage" DOUBLE PRECISION NOT NULL,
	"element_type" TEXT NULL DEFAULT NULL,
	"card_type" TEXT NULL DEFAULT NULL,
	PRIMARY KEY ("card_id"),
	CONSTRAINT "cards_card_type_check" CHECK ((card_type = ANY (ARRAY[('spell'::character varying)::text, ('monster'::character varying)::text])))
)
;
COMMENT ON COLUMN "cards"."card_id" IS '';
COMMENT ON COLUMN "cards"."name" IS '';
COMMENT ON COLUMN "cards"."damage" IS '';
COMMENT ON COLUMN "cards"."element_type" IS '';
COMMENT ON COLUMN "cards"."card_type" IS '';


CREATE TABLE "deck" (
	"user_id" INTEGER NOT NULL,
	"card_id" UUID NOT NULL,
	PRIMARY KEY ("user_id", "card_id"),
	CONSTRAINT "deck_card_id_fkey" FOREIGN KEY ("card_id") REFERENCES "cards" ("card_id") ON UPDATE NO ACTION ON DELETE CASCADE,
	CONSTRAINT "deck_user_id_fkey" FOREIGN KEY ("user_id") REFERENCES "users" ("user_id") ON UPDATE NO ACTION ON DELETE CASCADE
)
;
COMMENT ON COLUMN "deck"."user_id" IS '';
COMMENT ON COLUMN "deck"."card_id" IS '';


CREATE TABLE "packagecards" (
	"package_id" INTEGER NOT NULL,
	"card_id" UUID NOT NULL,
	PRIMARY KEY ("package_id", "card_id"),
	CONSTRAINT "packagecards_card_id_fkey" FOREIGN KEY ("card_id") REFERENCES "cards" ("card_id") ON UPDATE NO ACTION ON DELETE CASCADE,
	CONSTRAINT "packagecards_package_id_fkey" FOREIGN KEY ("package_id") REFERENCES "packages" ("package_id") ON UPDATE NO ACTION ON DELETE CASCADE
)
;
COMMENT ON COLUMN "packagecards"."package_id" IS '';
COMMENT ON COLUMN "packagecards"."card_id" IS '';


CREATE TABLE "packages" (
	"package_id" SERIAL NOT NULL,
	"price" INTEGER NULL DEFAULT 5,
	PRIMARY KEY ("package_id")
)
;
COMMENT ON COLUMN "packages"."package_id" IS '';
COMMENT ON COLUMN "packages"."price" IS '';


CREATE TABLE "trading_deals" (
	"id" UUID NOT NULL,
	"card_to_trade" UUID NOT NULL,
	"type" TEXT NOT NULL,
	"minimum_damage" DOUBLE PRECISION NOT NULL,
	"user_id" INTEGER NOT NULL,
	PRIMARY KEY ("id"),
	CONSTRAINT "trading_deals_card_to_trade_fkey" FOREIGN KEY ("card_to_trade") REFERENCES "cards" ("card_id") ON UPDATE NO ACTION ON DELETE CASCADE,
	CONSTRAINT "trading_deals_user_id_fkey" FOREIGN KEY ("user_id") REFERENCES "users" ("user_id") ON UPDATE NO ACTION ON DELETE CASCADE
)
;
COMMENT ON COLUMN "trading_deals"."id" IS '';
COMMENT ON COLUMN "trading_deals"."card_to_trade" IS '';
COMMENT ON COLUMN "trading_deals"."type" IS '';
COMMENT ON COLUMN "trading_deals"."minimum_damage" IS '';
COMMENT ON COLUMN "trading_deals"."user_id" IS '';


CREATE TABLE "usercards" (
	"user_id" INTEGER NOT NULL,
	"card_id" UUID NOT NULL,
	PRIMARY KEY ("user_id", "card_id"),
	CONSTRAINT "usercards_card_id_fkey" FOREIGN KEY ("card_id") REFERENCES "cards" ("card_id") ON UPDATE NO ACTION ON DELETE CASCADE,
	CONSTRAINT "usercards_user_id_fkey" FOREIGN KEY ("user_id") REFERENCES "users" ("user_id") ON UPDATE NO ACTION ON DELETE CASCADE
)
;
COMMENT ON COLUMN "usercards"."user_id" IS '';
COMMENT ON COLUMN "usercards"."card_id" IS '';


CREATE TABLE "users" (
	"user_id" SERIAL NOT NULL,
	"username" VARCHAR(50) NOT NULL,
	"password" TEXT NOT NULL,
	"coins" INTEGER NOT NULL DEFAULT 20,
	"token" TEXT NULL DEFAULT NULL,
	"bio" TEXT NULL DEFAULT NULL,
	"name" VARCHAR(100) NULL DEFAULT NULL::character varying,
	"image" TEXT NULL DEFAULT NULL,
	"elo" INTEGER NULL DEFAULT 100,
	"wins" INTEGER NOT NULL DEFAULT 0,
	"losses" INTEGER NOT NULL DEFAULT 0,
	"draws" INTEGER NOT NULL DEFAULT 0,
	PRIMARY KEY ("user_id"),
	UNIQUE "users_username_key" ("username")
)
;
COMMENT ON COLUMN "users"."user_id" IS '';
COMMENT ON COLUMN "users"."username" IS '';
COMMENT ON COLUMN "users"."password" IS '';
COMMENT ON COLUMN "users"."coins" IS '';
COMMENT ON COLUMN "users"."token" IS '';
COMMENT ON COLUMN "users"."bio" IS '';
COMMENT ON COLUMN "users"."name" IS '';
COMMENT ON COLUMN "users"."image" IS '';
COMMENT ON COLUMN "users"."elo" IS '';
COMMENT ON COLUMN "users"."wins" IS '';
COMMENT ON COLUMN "users"."losses" IS '';
COMMENT ON COLUMN "users"."draws" IS '';
