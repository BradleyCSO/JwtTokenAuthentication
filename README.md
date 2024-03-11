# JwtTokenAuthentication
API which issues JWT tokens based on valid users within a PostgreSQL database; which allows them to make certain API calls provided a valid token.

# Setup
The app uses Docker, containing a docker-compose file which containerises postgres, pgadmin4 and the app itself which can all be run via the following commands:
### Build
`docker-compose build`
### Run
`docker-compose up`