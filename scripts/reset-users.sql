-- Deletes all LinkNest users and their owned data so you can re-register.
-- Run in Neon SQL Editor or: psql "<connection-string>" -f scripts/reset-users.sql
--
-- WARNING: This removes ALL users and user-owned content. Groups with members are removed too.

BEGIN;

DELETE FROM meal_links;
DELETE FROM meal_categories;
DELETE FROM group_invites;
DELETE FROM group_memberships;
DELETE FROM groups;
DELETE FROM "AspNetUserTokens";
DELETE FROM "AspNetUserRoles";
DELETE FROM "AspNetUserLogins";
DELETE FROM "AspNetUserClaims";
DELETE FROM "AspNetUsers";

COMMIT;
