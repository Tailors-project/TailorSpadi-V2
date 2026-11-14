FROM node:22-alpine AS build
WORKDIR /source
COPY package.json package-lock.json ./
COPY src/Frontend/tailorspadi-admin/package.json ./src/Frontend/tailorspadi-admin/
COPY src/Frontend/tailorspadi-backoffice/package.json ./src/Frontend/tailorspadi-backoffice/
COPY src/Frontend/tailorspadi-storefront/package.json ./src/Frontend/tailorspadi-storefront/
COPY src/Frontend/tailorspadi-shared/package.json ./src/Frontend/tailorspadi-shared/
RUN npm ci
COPY src/Frontend ./src/Frontend
ARG APP_NAME
RUN npm run build --workspace ${APP_NAME}
RUN cp -r src/Frontend/${APP_NAME}/dist /site

FROM nginx:stable-alpine
ARG APP_NAME
COPY src/Frontend/${APP_NAME}/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /site /usr/share/nginx/html
EXPOSE 80
